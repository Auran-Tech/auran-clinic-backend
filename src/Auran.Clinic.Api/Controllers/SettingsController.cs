using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Lookups;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Settings;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/settings")]
[Produces("application/json")]
public sealed class SettingsController(
    IClinicSettingsService clinicSettingsService,
    ISystemLookupService systemLookupService,
    IValidator<UpdateClinicSettingsRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.View)]
    [SwaggerOperation(
        Summary = "Get clinic settings",
        Description = "Returns branding, localization, contact, prescription, reminder, and patient-number settings for the authenticated clinic.",
        OperationId = "Settings_Get",
        Tags = new[] { "Settings" })]
    public async Task<ActionResult<BaseResponse<ClinicSettingsResponse>>> Get(
        CancellationToken cancellationToken)
    {
        var settings = await clinicSettingsService.GetAsync(cancellationToken);
        if (settings is null)
        {
            return NotFound(new BaseResponse
            {
                Status = false,
                Message = "Clinic settings not found."
            });
        }

        return Ok(new BaseResponse<ClinicSettingsResponse>
        {
            Status = true,
            Data = settings
        });
    }

    [HttpPut]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    [SwaggerOperation(
        Summary = "Update clinic settings",
        Description = "Updates clinic-level branding, localization, contact, prescription, reminder, welcome, and patient-number settings.",
        OperationId = "Settings_Update",
        Tags = new[] { "Settings" })]
    public async Task<ActionResult<BaseResponse<ClinicSettingsResponse>>> Update(
        [FromBody] UpdateClinicSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new BaseResponse<ClinicSettingsResponse>
            {
                Status = false,
                Error = "validation_error"
            });
        }

        var result = await clinicSettingsService.UpdateAsync(request, cancellationToken);
        return result.Outcome switch
        {
            ClinicSettingsOutcome.Success => Ok(new BaseResponse<ClinicSettingsResponse>
            {
                Status = true,
                Data = result.Settings
            }),
            ClinicSettingsOutcome.NotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Clinic not found."
            }),
            ClinicSettingsOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to update clinic settings."
            })
        };
    }

    [HttpGet("lookups")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.View)]
    [SwaggerOperation(
        Summary = "Get settings lookups",
        Description = "Returns supported time zones and locales for clinic settings.",
        OperationId = "Settings_Lookups",
        Tags = new[] { "Settings" })]
    public ActionResult<BaseResponse<ClinicSettingsLookupsResponse>> Lookups()
    {
        var data = new ClinicSettingsLookupsResponse(
            systemLookupService.GetTimeZones(),
            systemLookupService.GetLocales());

        return Ok(new BaseResponse<ClinicSettingsLookupsResponse>
        {
            Status = true,
            Data = data
        });
    }
}
