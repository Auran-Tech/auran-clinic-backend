using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Clinics;
using Auran.Clinic.Application.Features;
using Auran.Clinic.Application.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Route("api/clinic")]
[Produces("application/json")]
[Authorize(Policy = ActorPolicies.Clinic)]
public sealed class ClinicController(
    IClinicService clinicService,
    IValidator<UpdateClinicSettingsRequest> settingsValidator) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Get current clinic", Description = "Returns the authenticated clinic's own tenant identity and configuration. The clinic is resolved from the JWT and cannot be selected using a route or query parameter.", OperationId = "Clinic_GetCurrent", Tags = new[] { "Clinic" })]
    public async Task<ActionResult<BaseResponse<ClinicDetailsResponse>>> GetCurrent(CancellationToken cancellationToken)
    {
        var result = await clinicService.GetCurrentAsync(cancellationToken);
        return result is null
            ? NotFound(new BaseResponse { Status = false, Message = "Current clinic was not found or is inactive." })
            : Ok(new BaseResponse<ClinicDetailsResponse> { Status = true, Data = result });
    }

    [HttpGet("settings")]
    [Authorize(Policy = PermissionPolicy.ForClinic(Permissions.Clinic.Settings.View))]
    [SwaggerOperation(Summary = "Get current clinic settings", Description = "Returns branding, localization, patient-numbering, welcome-page, prescription and reminder settings for the authenticated clinic only.", OperationId = "ClinicSettings_Get", Tags = new[] { "Clinic" })]
    public async Task<ActionResult<BaseResponse<ClinicSettingsResponse>>> GetSettings(CancellationToken cancellationToken)
    {
        var result = await clinicService.GetSettingsAsync(cancellationToken);
        return result is null
            ? NotFound(new BaseResponse { Status = false, Message = "Clinic settings were not found." })
            : Ok(new BaseResponse<ClinicSettingsResponse> { Status = true, Data = result });
    }

    [HttpPut("settings")]
    [Authorize(Policy = PermissionPolicy.ForClinic(Permissions.Clinic.Settings.Manage))]
    [SwaggerOperation(Summary = "Update current clinic settings", Description = "Updates configurable settings for the authenticated clinic only. ClinicId is never accepted from the request and all mutations are centrally audited.", OperationId = "ClinicSettings_Update", Tags = new[] { "Clinic" })]
    public async Task<ActionResult<BaseResponse<ClinicSettingsResponse>>> UpdateSettings([FromBody] UpdateClinicSettingsRequest request, CancellationToken cancellationToken)
    {
        var validation = await settingsValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse { Status = false, Message = "Validation failed.", Error = string.Join(" ", validation.Errors.Select(x => x.ErrorMessage)) });
        var result = await clinicService.UpdateSettingsAsync(request, cancellationToken);
        return result is null
            ? NotFound(new BaseResponse { Status = false, Message = "Clinic settings were not found." })
            : Ok(new BaseResponse<ClinicSettingsResponse> { Status = true, Message = "Clinic settings updated successfully.", Data = result });
    }

    [HttpGet("features")]
    [SwaggerOperation(Summary = "Get current clinic features", Description = "Returns the feature entitlements enabled for the authenticated clinic. Clinic users can read feature availability but cannot change it.", OperationId = "ClinicFeatures_GetCurrent", Tags = new[] { "Clinic" })]
    public async Task<ActionResult<BaseResponse<IReadOnlyCollection<ClinicFeatureResponse>>>> GetFeatures(CancellationToken cancellationToken)
    {
        var result = await clinicService.GetFeaturesAsync(cancellationToken);
        return result is null
            ? NotFound(new BaseResponse { Status = false, Message = "Clinic features were not found." })
            : Ok(new BaseResponse<IReadOnlyCollection<ClinicFeatureResponse>> { Status = true, Data = result });
    }
}
