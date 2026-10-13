using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Workflow;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/workflow-config")]
[Produces("application/json")]
public sealed class WorkflowConfigurationController(
    IWorkflowConfigurationService service,
    IValidator<CreateWorkflowStatusRequest> createValidator,
    IValidator<UpdateWorkflowStatusRequest> updateValidator,
    IValidator<DeleteWorkflowStatusRequest> deleteValidator,
    IValidator<ReplaceWorkflowTransitionsRequest> transitionsValidator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.View)]
    [SwaggerOperation(
        Summary = "Get clinic workflow configuration",
        Description = "Returns tenant-scoped queue statuses, in-use flags, and allowed transitions.",
        OperationId = "WorkflowConfiguration_Get",
        Tags = new[] { "Settings", "Workflow" })]
    public async Task<ActionResult<BaseResponse<WorkflowConfigurationResponse>>> Get(
        CancellationToken cancellationToken)
    {
        var data = await service.GetAsync(cancellationToken);
        return Ok(new BaseResponse<WorkflowConfigurationResponse>
        {
            Status = true,
            Data = data
        });
    }

    [HttpPost("statuses")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<WorkflowConfigurationResponse>>> CreateStatus(
        [FromBody] CreateWorkflowStatusRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure();

        return Map(await service.CreateStatusAsync(request, cancellationToken), created: true);
    }

    [HttpPut("statuses")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<WorkflowConfigurationResponse>>> UpdateStatus(
        [FromBody] UpdateWorkflowStatusRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure();

        return Map(await service.UpdateStatusAsync(request, cancellationToken));
    }

    [HttpDelete("statuses")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<WorkflowConfigurationResponse>>> DeleteStatus(
        [FromBody] DeleteWorkflowStatusRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await deleteValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure();

        return Map(await service.DeleteStatusAsync(request, cancellationToken));
    }

    [HttpPut("transitions")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<WorkflowConfigurationResponse>>> ReplaceTransitions(
        [FromBody] ReplaceWorkflowTransitionsRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await transitionsValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure();

        return Map(await service.ReplaceTransitionsAsync(request, cancellationToken));
    }

    private ActionResult<BaseResponse<WorkflowConfigurationResponse>> Map(
        WorkflowConfigurationResult result,
        bool created = false)
    {
        return result.Outcome switch
        {
            WorkflowConfigurationOutcome.Success when created => StatusCode(
                StatusCodes.Status201Created,
                new BaseResponse<WorkflowConfigurationResponse>
                {
                    Status = true,
                    Data = result.Configuration
                }),
            WorkflowConfigurationOutcome.Success => Ok(new BaseResponse<WorkflowConfigurationResponse>
            {
                Status = true,
                Data = result.Configuration
            }),
            WorkflowConfigurationOutcome.NotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Workflow status not found."
            }),
            WorkflowConfigurationOutcome.ValidationError => BadRequest(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Invalid workflow configuration.",
                Error = "workflow_validation_error"
            }),
            WorkflowConfigurationOutcome.Conflict => Conflict(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Workflow configuration conflicts with existing data.",
                Error = "workflow_conflict"
            }),
            WorkflowConfigurationOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to manage workflow configuration."
            })
        };
    }

    private BadRequestObjectResult ValidationFailure() =>
        BadRequest(new BaseResponse<WorkflowConfigurationResponse>
        {
            Status = false,
            Error = "validation_error"
        });
}
