using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Queue;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/queue")]
[Produces("application/json")]
public sealed class QueueController(
    IQueueService queueService,
    IValidator<QueueQuery> queryValidator,
    IValidator<MoveQueueEntryRequest> moveValidator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Queue.View)]
    [SwaggerOperation(
        Summary = "List active clinic queue",
        Description = "Returns active clinic queue entries with validated server sorting. includeRecentExits=true also returns entries exited in the last 24 hours.",
        OperationId = "Queue_ListActive",
        Tags = new[] { "Queue" })]
    public async Task<ActionResult<BaseResponse<IReadOnlyList<QueueEntryResponse>>>> List(
        [FromQuery] QueueQuery query,
        CancellationToken cancellationToken)
    {
        var validation = await queryValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new BaseResponse<IReadOnlyList<QueueEntryResponse>>
            {
                Status = false,
                Error = "validation_error"
            });
        }

        var data = await queueService.ListActiveAsync(query, cancellationToken);
        return Ok(new BaseResponse<IReadOnlyList<QueueEntryResponse>>
        {
            Status = true,
            Data = data
        });
    }

    [HttpGet("transitions")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Queue.View)]
    [SwaggerOperation(
        Summary = "List allowed queue transitions",
        Description = "Returns only workflow statuses directly reachable from the queue entry's current status.",
        OperationId = "Queue_ListTransitions",
        Tags = new[] { "Queue" })]
    public async Task<ActionResult<BaseResponse<IReadOnlyList<QueueTransitionOptionResponse>>>> Transitions(
        [FromQuery] Guid queueEntryId,
        CancellationToken cancellationToken)
    {
        if (queueEntryId == Guid.Empty)
        {
            return BadRequest(new BaseResponse<IReadOnlyList<QueueTransitionOptionResponse>>
            {
                Status = false,
                Error = "validation_error"
            });
        }

        var data = await queueService.GetAvailableTransitionsAsync(queueEntryId, cancellationToken);
        return Ok(new BaseResponse<IReadOnlyList<QueueTransitionOptionResponse>>
        {
            Status = true,
            Data = data
        });
    }

    [HttpPut("move")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Queue.Move)]
    [SwaggerOperation(
        Summary = "Move a queue entry",
        Description = "Moves a patient only through a configured workflow transition and records queue status history. Final statuses close the queue entry and visit.",
        OperationId = "Queue_Move",
        Tags = new[] { "Queue" })]
    public async Task<ActionResult<BaseResponse<QueueEntryResponse>>> Move(
        [FromBody] MoveQueueEntryRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await moveValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new BaseResponse<QueueEntryResponse>
            {
                Status = false,
                Error = "validation_error"
            });
        }

        var result = await queueService.MoveAsync(request, cancellationToken);
        return result.Outcome switch
        {
            QueueMoveOutcome.Success => Ok(new BaseResponse<QueueEntryResponse>
            {
                Status = true,
                Data = result.Entry
            }),
            QueueMoveOutcome.NotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Queue entry not found."
            }),
            QueueMoveOutcome.InvalidTransition => Conflict(new BaseResponse
            {
                Status = false,
                Message = "The requested workflow transition is not allowed.",
                Error = "invalid_transition"
            }),
            QueueMoveOutcome.AlreadyExited => Conflict(new BaseResponse
            {
                Status = false,
                Message = "The queue entry has already exited.",
                Error = "queue_entry_exited"
            }),
            QueueMoveOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to move the queue entry."
            })
        };
    }
}
