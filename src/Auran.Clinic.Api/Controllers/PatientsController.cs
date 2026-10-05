using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Patients;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/patients")]
[Produces("application/json")]
public sealed class PatientsController(
    IPatientService patientService,
    IValidator<CreatePatientRequest> createValidator,
    IValidator<UpdatePatientRequest> updateValidator,
    IValidator<PatientDuplicateCheckRequest> duplicateValidator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.View)]
    [SwaggerOperation(
        Summary = "List clinic patients",
        Description = "Returns a tenant-scoped paginated patient list. Search matches patient number, full name, or phone.",
        OperationId = "Patients_List",
        Tags = new[] { "Patients" })]
    public async Task<ActionResult<BaseResponse<PaginatedResponse<PatientResponse>>>> List(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await patientService.ListAsync(search, page, pageSize, cancellationToken);
        return Ok(new BaseResponse<PaginatedResponse<PatientResponse>>
        {
            Status = true,
            Data = result
        });
    }

    [HttpGet("details")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.View)]
    [SwaggerOperation(
        Summary = "Get patient details",
        Description = "Returns one patient from the authenticated clinic.",
        OperationId = "Patients_Get",
        Tags = new[] { "Patients" })]
    public async Task<ActionResult<BaseResponse<PatientResponse>>> Get(
        [FromQuery] Guid patientId,
        CancellationToken cancellationToken)
    {
        var patient = await patientService.GetAsync(patientId, cancellationToken);
        if (patient is null)
        {
            return NotFound(new BaseResponse
            {
                Status = false,
                Message = "Patient not found."
            });
        }

        return Ok(new BaseResponse<PatientResponse> { Status = true, Data = patient });
    }

    [HttpPost("duplicates")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.View)]
    [SwaggerOperation(
        Summary = "Check potential patient duplicates",
        Description = "Checks the authenticated clinic for an exact phone match and, when date of birth is supplied, a matching full-name/date-of-birth pair.",
        OperationId = "Patients_CheckDuplicates",
        Tags = new[] { "Patients" })]
    public async Task<ActionResult<BaseResponse<PatientDuplicateCheckResponse>>> CheckDuplicates(
        [FromBody] PatientDuplicateCheckRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await duplicateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailure<PatientDuplicateCheckResponse>();

        var result = await patientService.CheckDuplicatesAsync(request, cancellationToken);
        return Ok(new BaseResponse<PatientDuplicateCheckResponse> { Status = true, Data = result });
    }

    [HttpPost]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.Create)]
    [SwaggerOperation(
        Summary = "Create a patient",
        Description = "Creates a patient inside the authenticated clinic and generates a clinic-scoped patient number. Exact duplicate phone numbers are rejected.",
        OperationId = "Patients_Create",
        Tags = new[] { "Patients" })]
    public async Task<ActionResult<BaseResponse<PatientResponse>>> Create(
        [FromBody] CreatePatientRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailure<PatientResponse>();

        return MapMutation(await patientService.CreateAsync(request, cancellationToken), created: true);
    }

    [HttpPut]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.Update)]
    [SwaggerOperation(
        Summary = "Update basic patient details",
        Description = "Updates basic patient information inside the authenticated clinic. Patient number and clinic ownership cannot be changed.",
        OperationId = "Patients_Update",
        Tags = new[] { "Patients" })]
    public async Task<ActionResult<BaseResponse<PatientResponse>>> Update(
        [FromBody] UpdatePatientRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailure<PatientResponse>();

        return MapMutation(await patientService.UpdateAsync(request.PatientId, request, cancellationToken));
    }

    private ActionResult<BaseResponse<PatientResponse>> MapMutation(
        PatientMutationResult result,
        bool created = false)
    {
        return result.Outcome switch
        {
            PatientMutationOutcome.Success when created => StatusCode(
                StatusCodes.Status201Created,
                new BaseResponse<PatientResponse> { Status = true, Data = result.Patient }),
            PatientMutationOutcome.Success => Ok(new BaseResponse<PatientResponse>
            {
                Status = true,
                Data = result.Patient
            }),
            PatientMutationOutcome.NotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Patient not found."
            }),
            PatientMutationOutcome.Conflict => Conflict(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Patient data conflicts with an existing patient.",
                Error = "patient_conflict"
            }),
            PatientMutationOutcome.ValidationError => BadRequest(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Validation failed.",
                Error = "validation_error"
            }),
            PatientMutationOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to process the patient request."
            })
        };
    }

    private BadRequestObjectResult ValidationFailure<T>() where T : class =>
        BadRequest(new BaseResponse<T>
        {
            Status = false,
            Error = "validation_error"
        });
}
