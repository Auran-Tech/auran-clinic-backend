using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.PatientMedicalHistory;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/patient-medical-history")]
[Produces("application/json")]
public sealed class PatientMedicalHistoryController(
    IPatientMedicalHistoryService service,
    IValidator<CreatePatientConditionRequest> conditionValidator,
    IValidator<CreatePatientAllergyRequest> allergyValidator,
    IValidator<CreatePatientMedicationRequest> medicationValidator,
    IValidator<DeletePatientMedicalHistoryItemRequest> deleteValidator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.View)]
    [SwaggerOperation(
        Summary = "Get patient medical history",
        Description = "Returns current conditions, allergies, and medications for a patient in the authenticated clinic.",
        OperationId = "PatientMedicalHistory_Get",
        Tags = new[] { "Patients" })]
    public async Task<ActionResult<BaseResponse<PatientMedicalHistoryResponse>>> Get(
        [FromQuery] Guid patientId,
        CancellationToken cancellationToken)
    {
        if (patientId == Guid.Empty)
            return ValidationFailure<PatientMedicalHistoryResponse>();

        var history = await service.GetAsync(patientId, cancellationToken);
        if (history is null)
        {
            return NotFound(new BaseResponse
            {
                Status = false,
                Message = "Patient not found."
            });
        }

        return Ok(new BaseResponse<PatientMedicalHistoryResponse>
        {
            Status = true,
            Data = history
        });
    }

    [HttpPost("conditions")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.Update)]
    public async Task<ActionResult<BaseResponse<PatientMedicalHistoryResponse>>> AddCondition(
        [FromBody] CreatePatientConditionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await conditionValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure<PatientMedicalHistoryResponse>();

        return Map(await service.AddConditionAsync(request, cancellationToken), created: true);
    }

    [HttpDelete("conditions")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.Update)]
    public async Task<ActionResult<BaseResponse<PatientMedicalHistoryResponse>>> DeleteCondition(
        [FromBody] DeletePatientMedicalHistoryItemRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await deleteValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure<PatientMedicalHistoryResponse>();

        return Map(await service.DeleteConditionAsync(request, cancellationToken));
    }

    [HttpPost("allergies")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.Update)]
    public async Task<ActionResult<BaseResponse<PatientMedicalHistoryResponse>>> AddAllergy(
        [FromBody] CreatePatientAllergyRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await allergyValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure<PatientMedicalHistoryResponse>();

        return Map(await service.AddAllergyAsync(request, cancellationToken), created: true);
    }

    [HttpDelete("allergies")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.Update)]
    public async Task<ActionResult<BaseResponse<PatientMedicalHistoryResponse>>> DeleteAllergy(
        [FromBody] DeletePatientMedicalHistoryItemRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await deleteValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure<PatientMedicalHistoryResponse>();

        return Map(await service.DeleteAllergyAsync(request, cancellationToken));
    }

    [HttpPost("medications")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.Update)]
    public async Task<ActionResult<BaseResponse<PatientMedicalHistoryResponse>>> AddMedication(
        [FromBody] CreatePatientMedicationRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await medicationValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure<PatientMedicalHistoryResponse>();

        return Map(await service.AddMedicationAsync(request, cancellationToken), created: true);
    }

    [HttpDelete("medications")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.Update)]
    public async Task<ActionResult<BaseResponse<PatientMedicalHistoryResponse>>> DeleteMedication(
        [FromBody] DeletePatientMedicalHistoryItemRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await deleteValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure<PatientMedicalHistoryResponse>();

        return Map(await service.DeleteMedicationAsync(request, cancellationToken));
    }

    private ActionResult<BaseResponse<PatientMedicalHistoryResponse>> Map(
        PatientMedicalHistoryResult result,
        bool created = false)
    {
        return result.Outcome switch
        {
            PatientMedicalHistoryOutcome.Success when created => StatusCode(
                StatusCodes.Status201Created,
                new BaseResponse<PatientMedicalHistoryResponse>
                {
                    Status = true,
                    Data = result.History
                }),
            PatientMedicalHistoryOutcome.Success => Ok(
                new BaseResponse<PatientMedicalHistoryResponse>
                {
                    Status = true,
                    Data = result.History
                }),
            PatientMedicalHistoryOutcome.PatientNotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Patient not found."
            }),
            PatientMedicalHistoryOutcome.ItemNotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Medical history item not found."
            }),
            PatientMedicalHistoryOutcome.Conflict => Conflict(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "This medical history item is already recorded.",
                Error = "medical_history_conflict"
            }),
            PatientMedicalHistoryOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to manage patient medical history."
            })
        };
    }

    private BadRequestObjectResult ValidationFailure<T>() =>
        BadRequest(new BaseResponse<T>
        {
            Status = false,
            Error = "validation_error"
        });
}
