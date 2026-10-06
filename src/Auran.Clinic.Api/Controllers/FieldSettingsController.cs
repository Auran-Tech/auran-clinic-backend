using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Settings;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/settings/fields")]
[Produces("application/json")]
public sealed class FieldSettingsController(
    IFieldSettingsService fieldSettingsService,
    IValidator<SaveFieldSettingsRequest> validator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.View)]
    public async Task<ActionResult<BaseResponse<FieldSettingsResponse>>> Get(
        CancellationToken cancellationToken)
    {
        var result = await fieldSettingsService.GetAsync(cancellationToken);
        return Ok(new BaseResponse<FieldSettingsResponse> { Status = true, Data = result });
    }

    [HttpPut]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<FieldSettingsResponse>>> Save(
        [FromBody] SaveFieldSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<FieldSettingsResponse> { Status = false, Error = "validation_error" });

        var result = await fieldSettingsService.SaveAsync(request, cancellationToken);
        if (result is null)
            return BadRequest(new BaseResponse<FieldSettingsResponse>
            {
                Status = false,
                Message = "Field configuration contains invalid identifiers.",
                Error = "invalid_field_configuration"
            });

        return Ok(new BaseResponse<FieldSettingsResponse> { Status = true, Data = result });
    }
}
