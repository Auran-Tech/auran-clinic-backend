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
    IValidator<PatientQuery> queryValidator,
    IValidator<CreatePatientRequest> createValidator,
    IValidator<UpdatePatientRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.View)]
    [SwaggerOperation(
        Summary = "List clinic patients",
        Description = "Returns a tenant-scoped paginated patient list. Search matches name, phone, or patient number.",
        OperationId = "Patients_List",
        Tags = new[] { "Patients" })]
    public async Task<ActionResult<BaseResponse<PaginatedResponse<PatientResponse>>>> List(
        [FromQuery] PatientQuery query,
        CancellationToken cancellationToken)
    {
        var validation = await queryValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailure<PaginatedResponse<PatientResponse>>();

        var result = await patientService.ListAsync(query, cancellationToken);
        return Ok(new BaseResponse<PaginatedResponse<PatientResponse>>
        {
            Status = true,
            Data = result
        });
    }

    [HttpGet("{patientId:guid}")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.View)]
    [SwaggerOperation(
        Summary = "Get a clinic patient",
        Description = "Returns one patient from the authenticated clinic. Cross-clinic records are fail-closed by the persistence boundary.",
        OperationId = "Patients_Get",
        Tags = new[] { "Patients" })]
    public async Task<ActionResult<BaseResponse<PatientResponse>>> Get(
        Guid patientId,
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

        return Ok(new BaseResponse<PatientResponse>
        {
            Status = true,
            Data = patient
        });
    }

    [HttpPost("duplicates")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.View)]
    [SwaggerOperation(
        Summary = "Find possible duplicate patients",
        Description = "Checks the authenticated clinic for possible duplicates using normalized phone and name/date-of-birth matching.",
        OperationId = "Patients_FindDuplicates",
        Tags = new[] { "Patients" })]
    public async Task<ActionResult<BaseResponse<IReadOnlyList<PatientDuplicateCandidateResponse>>>> FindDuplicates(
        [FromBody] CreatePatientRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailure<IReadOnlyList<PatientDuplicateCandidateResponse>>();

        var matches = await patientService.FindDuplicatesAsync(request, cancellationToken);
        return Ok(new BaseResponse<IReadOnlyList<PatientDuplicateCandidateResponse>>
        {
            Status = true,
            Data = matches
        });
    }

    [HttpPost]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.Create)]
    [SwaggerOperation(
        Summary = "Create a clinic patient",
        Description = "Creates a patient in the authenticated clinic and generates a clinic-scoped patient number.",
        OperationId = "Patients_Create",
        Tags = new[] { "Patients" })]
    public async Task<ActionResult<BaseResponse<PatientResponse>>> Create(
        [FromBody] CreatePatientRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailure<PatientResponse>();

        return MapManagementResult(
            await patientService.CreateAsync(request, cancellationToken),
            created: true);
    }

    [HttpPut]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.Update)]
    [SwaggerOperation(
        Summary = "Update patient basic information",
        Description = "Updates basic patient information in the authenticated clinic. The patient identifier is supplied in the request body.",
        OperationId = "Patients_Update",
        Tags = new[] { "Patients" })]
    public async Task<ActionResult<BaseResponse<PatientResponse>>> Update(
        [FromBody] UpdatePatientRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailure<PatientResponse>();

        return MapManagementResult(
            await patientService.UpdateAsync(request, cancellationToken));
    }

    private ActionResult<BaseResponse<PatientResponse>> MapManagementResult(
        PatientManagementResult result,
        bool created = false)
    {
        return result.Outcome switch
        {
            PatientManagementOutcome.Success when created => StatusCode(
                StatusCodes.Status201Created,
                new BaseResponse<PatientResponse> { Status = true, Data = result.Patient }),
            PatientManagementOutcome.Success => Ok(new BaseResponse<PatientResponse>
            {
                Status = true,
                Data = result.Patient
            }),
            PatientManagementOutcome.NotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Patient not found."
            }),
            PatientManagementOutcome.Conflict => Conflict(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "The patient conflicts with existing data.",
                Error = "patient_conflict"
            }),
            PatientManagementOutcome.ValidationError => BadRequest(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Validation failed.",
                Error = "validation_error"
            }),
            PatientManagementOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to manage the patient."
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
