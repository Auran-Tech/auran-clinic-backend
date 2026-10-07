using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.PatientProfiles;
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
    IPatientProfileService patientProfileService,
    IValidator<SavePatientProfileRequest> saveValidator) : ControllerBase
{
    [HttpGet("configuration")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.View)]
    [SwaggerOperation(
        Summary = "Get dynamic patient profile configuration",
        Description = "Returns enabled clinic patient-profile sections, fields, types, required flags, and select options.",
        OperationId = "PatientProfile_GetConfiguration",
        Tags = new[] { "Patients" })]
    public async Task<ActionResult<BaseResponse<PatientProfileConfigurationResponse>>> Configuration(
        CancellationToken cancellationToken)
    {
        var data = await patientProfileService.GetConfigurationAsync(cancellationToken);
        return Ok(new BaseResponse<PatientProfileConfigurationResponse>
        {
            Status = true,
            Data = data
        });
    }

    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.View)]
    [SwaggerOperation(
        Summary = "Get patient dynamic profile values",
        Description = "Returns typed dynamic profile values for a patient in the authenticated clinic.",
        OperationId = "PatientProfile_Get",
        Tags = new[] { "Patients" })]
    public async Task<ActionResult<BaseResponse<PatientProfileResponse>>> Get(
        [FromQuery] Guid patientId,
        CancellationToken cancellationToken)
    {
        if (patientId == Guid.Empty)
        {
            return BadRequest(new BaseResponse<PatientProfileResponse>
            {
                Status = false,
                Error = "validation_error"
            });
        }

        var data = await patientProfileService.GetPatientProfileAsync(patientId, cancellationToken);
        if (data is null)
        {
            return NotFound(new BaseResponse
            {
                Status = false,
                Message = "Patient not found."
            });
        }

        return Ok(new BaseResponse<PatientProfileResponse>
        {
            Status = true,
            Data = data
        });
    }

    [HttpPut]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.Update)]
    [SwaggerOperation(
        Summary = "Save patient dynamic profile values",
        Description = "Upserts typed values for editable dynamic fields. Required enabled editable fields must be populated. Image/File values are preserved and managed separately.",
        OperationId = "PatientProfile_Save",
        Tags = new[] { "Patients" })]
    public async Task<ActionResult<BaseResponse<PatientProfileResponse>>> Save(
        [FromBody] SavePatientProfileRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await saveValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new BaseResponse<PatientProfileResponse>
            {
                Status = false,
                Error = "validation_error"
            });
        }

        var result = await patientProfileService.SaveAsync(request, cancellationToken);
        return result.Outcome switch
        {
            PatientProfileOutcome.Success => Ok(new BaseResponse<PatientProfileResponse>
            {
                Status = true,
                Data = result.Profile
            }),
            PatientProfileOutcome.PatientNotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Patient not found."
            }),
            PatientProfileOutcome.InvalidField => BadRequest(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Invalid patient profile field.",
                Error = "invalid_profile_field"
            }),
            PatientProfileOutcome.InvalidValue => BadRequest(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Invalid patient profile value.",
                Error = "invalid_profile_value"
            }),
            PatientProfileOutcome.RequiredValueMissing => BadRequest(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "A required patient profile value is missing.",
                Error = "required_profile_value_missing"
            }),
            PatientProfileOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to save patient profile."
            })
        };
    }
}
