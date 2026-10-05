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
[Route("api/patient-profile")]
[Produces("application/json")]
public sealed class PatientProfileController(
    IPatientClinicalProfileService profileService,
    IValidator<AddPatientAllergyRequest> allergyValidator,
    IValidator<AddPatientConditionRequest> conditionValidator,
    IValidator<AddPatientMedicationRequest> medicationValidator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.MedicalProfile.View)]
    [SwaggerOperation(
        Summary = "Get patient clinical profile",
        Description = "Returns the patient basic record with allergies, conditions and medications for the authenticated clinic.",
        OperationId = "PatientProfile_Get",
        Tags = new[] { "Patient Profile" })]
    public async Task<ActionResult<BaseResponse<PatientClinicalProfileResponse>>> Get(
        [FromQuery] Guid patientId,
        CancellationToken cancellationToken)
    {
        var profile = await profileService.GetAsync(patientId, cancellationToken);
        if (profile is null)
            return NotFound(new BaseResponse { Status = false, Message = "Patient not found." });

        return Ok(new BaseResponse<PatientClinicalProfileResponse> { Status = true, Data = profile });
    }

    [HttpPost("allergies")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.MedicalProfile.Edit)]
    public async Task<ActionResult<BaseResponse<PatientAllergyResponse>>> AddAllergy(
        [FromBody] AddPatientAllergyRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await allergyValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<PatientAllergyResponse> { Status = false, Error = "validation_error" });

        var result = await profileService.AddAllergyAsync(request, cancellationToken);
        if (result is null)
            return NotFound(new BaseResponse { Status = false, Message = "Patient not found." });

        return StatusCode(StatusCodes.Status201Created, new BaseResponse<PatientAllergyResponse> { Status = true, Data = result });
    }

    [HttpPost("conditions")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.MedicalProfile.Edit)]
    public async Task<ActionResult<BaseResponse<PatientConditionResponse>>> AddCondition(
        [FromBody] AddPatientConditionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await conditionValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<PatientConditionResponse> { Status = false, Error = "validation_error" });

        var result = await profileService.AddConditionAsync(request, cancellationToken);
        if (result is null)
            return NotFound(new BaseResponse { Status = false, Message = "Patient not found." });

        return StatusCode(StatusCodes.Status201Created, new BaseResponse<PatientConditionResponse> { Status = true, Data = result });
    }

    [HttpPost("medications")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.MedicalProfile.Edit)]
    public async Task<ActionResult<BaseResponse<PatientMedicationResponse>>> AddMedication(
        [FromBody] AddPatientMedicationRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await medicationValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<PatientMedicationResponse> { Status = false, Error = "validation_error" });

        var result = await profileService.AddMedicationAsync(request, cancellationToken);
        if (result is null)
            return NotFound(new BaseResponse { Status = false, Message = "Patient not found." });

        return StatusCode(StatusCodes.Status201Created, new BaseResponse<PatientMedicationResponse> { Status = true, Data = result });
    }
}
