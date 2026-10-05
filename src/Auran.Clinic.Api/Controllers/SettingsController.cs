using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Settings;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/settings")]
[Produces("application/json")]
public sealed class SettingsController(
    IClinicSettingsService clinicSettingsService,
    IWorkflowSettingsService workflowSettingsService,
    IValidator<UpdateClinicSettingsRequest> clinicSettingsValidator,
    IValidator<SaveWorkflowSettingsRequest> workflowSettingsValidator) : ControllerBase
{
    [HttpGet("clinic")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.View)]
    public async Task<ActionResult<BaseResponse<ClinicSettingsResponse>>> GetClinic(
        CancellationToken cancellationToken)
    {
        var settings = await clinicSettingsService.GetAsync(cancellationToken);
        if (settings is null)
            return NotFound(new BaseResponse { Status = false, Message = "Clinic settings not found." });

        return Ok(new BaseResponse<ClinicSettingsResponse> { Status = true, Data = settings });
    }

    [HttpPut("clinic")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<ClinicSettingsResponse>>> UpdateClinic(
        [FromBody] UpdateClinicSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await clinicSettingsValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<ClinicSettingsResponse> { Status = false, Error = "validation_error" });

        var settings = await clinicSettingsService.UpdateAsync(request, cancellationToken);
        if (settings is null)
            return NotFound(new BaseResponse { Status = false, Message = "Clinic settings not found." });

        return Ok(new BaseResponse<ClinicSettingsResponse> { Status = true, Data = settings });
    }

    [HttpGet("workflow")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.View)]
    public async Task<ActionResult<BaseResponse<WorkflowSettingsResponse>>> GetWorkflow(
        CancellationToken cancellationToken)
    {
        var settings = await workflowSettingsService.GetAsync(cancellationToken);
        return Ok(new BaseResponse<WorkflowSettingsResponse> { Status = true, Data = settings });
    }

    [HttpPut("workflow")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<WorkflowSettingsResponse>>> SaveWorkflow(
        [FromBody] SaveWorkflowSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await workflowSettingsValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<WorkflowSettingsResponse> { Status = false, Error = "validation_error" });

        var result = await workflowSettingsService.SaveAsync(request, cancellationToken);
        if (result.Settings is null)
        {
            return Conflict(new BaseResponse<WorkflowSettingsResponse>
            {
                Status = false,
                Message = result.Error ?? "Workflow configuration could not be saved.",
                Error = "workflow_configuration_conflict"
            });
        }

        return Ok(new BaseResponse<WorkflowSettingsResponse>
        {
            Status = true,
            Data = result.Settings
        });
    }
}
