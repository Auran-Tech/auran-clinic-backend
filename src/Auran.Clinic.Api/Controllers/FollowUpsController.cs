using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.FollowUps;
using Auran.Clinic.Application.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/follow-ups")]
[Produces("application/json")]
public sealed class FollowUpsController(
    IFollowUpService followUpService,
    IValidator<CreateFollowUpRequest> createValidator,
    IValidator<ChangeFollowUpStatusRequest> statusValidator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.FollowUps.View)]
    [SwaggerOperation(
        Summary = "List patient follow-ups",
        Description = "Returns follow-ups grouped by computed bucket: Today, Upcoming, Overdue, Completed, or All.",
        OperationId = "FollowUps_List",
        Tags = new[] { "Follow Ups" })]
    public async Task<ActionResult<BaseResponse<IReadOnlyList<FollowUpResponse>>>> List(
        [FromQuery] FollowUpQuery query,
        CancellationToken cancellationToken)
    {
        var data = await followUpService.ListAsync(query, cancellationToken);
        return Ok(new BaseResponse<IReadOnlyList<FollowUpResponse>>
        {
            Status = true,
            Data = data
        });
    }

    [HttpPost]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.FollowUps.Manage)]
    [SwaggerOperation(
        Summary = "Create a patient follow-up",
        Description = "Creates an open follow-up for a visit. Assigned doctor or Clinic Super User only.",
        OperationId = "FollowUps_Create",
        Tags = new[] { "Follow Ups" })]
    public async Task<ActionResult<BaseResponse<FollowUpResponse>>> Create(
        [FromBody] CreateFollowUpRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new BaseResponse<FollowUpResponse>
            {
                Status = false,
                Error = "validation_error"
            });
        }

        return Map(await followUpService.CreateAsync(request, cancellationToken), created: true);
    }

    [HttpPut("complete")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.FollowUps.Manage)]
    [SwaggerOperation(
        Summary = "Complete a follow-up",
        OperationId = "FollowUps_Complete",
        Tags = new[] { "Follow Ups" })]
    public async Task<ActionResult<BaseResponse<FollowUpResponse>>> Complete(
        [FromBody] ChangeFollowUpStatusRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await statusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailure();

        return Map(await followUpService.CompleteAsync(request, cancellationToken));
    }

    [HttpPut("cancel")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.FollowUps.Manage)]
    [SwaggerOperation(
        Summary = "Cancel a follow-up",
        OperationId = "FollowUps_Cancel",
        Tags = new[] { "Follow Ups" })]
    public async Task<ActionResult<BaseResponse<FollowUpResponse>>> Cancel(
        [FromBody] ChangeFollowUpStatusRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await statusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailure();

        return Map(await followUpService.CancelAsync(request, cancellationToken));
    }

    private ActionResult<BaseResponse<FollowUpResponse>> Map(
        FollowUpResult result,
        bool created = false)
    {
        return result.Outcome switch
        {
            FollowUpOutcome.Success when created => StatusCode(
                StatusCodes.Status201Created,
                new BaseResponse<FollowUpResponse> { Status = true, Data = result.FollowUp }),
            FollowUpOutcome.Success => Ok(new BaseResponse<FollowUpResponse>
            {
                Status = true,
                Data = result.FollowUp
            }),
            FollowUpOutcome.VisitNotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Visit not found."
            }),
            FollowUpOutcome.FollowUpNotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Follow-up not found."
            }),
            FollowUpOutcome.Forbidden => StatusCode(
                StatusCodes.Status403Forbidden,
                new BaseResponse
                {
                    Status = false,
                    Message = "Only the assigned doctor or a Clinic Super User can manage this follow-up."
                }),
            FollowUpOutcome.InvalidState => Conflict(new BaseResponse
            {
                Status = false,
                Message = "The follow-up is no longer open.",
                Error = "follow_up_not_open"
            }),
            FollowUpOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to manage follow-up."
            })
        };
    }

    private BadRequestObjectResult ValidationFailure() =>
        BadRequest(new BaseResponse<FollowUpResponse>
        {
            Status = false,
            Error = "validation_error"
        });
}
