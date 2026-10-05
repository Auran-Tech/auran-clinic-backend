using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Queue;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/queue")]
[Produces("application/json")]
public sealed class QueueController(
    IQueueService queueService,
    IValidator<QueueCheckInRequest> checkInValidator,
    IValidator<QueueMoveRequest> moveValidator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Queue.View)]
    public async Task<ActionResult<BaseResponse<QueueBoardResponse>>> GetBoard(
        CancellationToken cancellationToken)
    {
        var board = await queueService.GetBoardAsync(cancellationToken);
        return Ok(new BaseResponse<QueueBoardResponse>
        {
            Status = true,
            Data = board
        });
    }

    [HttpPost("check-in")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Queue.Move)]
    public async Task<ActionResult<BaseResponse<QueueEntryResponse>>> CheckIn(
        [FromBody] QueueCheckInRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await checkInValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<QueueEntryResponse> { Status = false, Error = "validation_error" });

        return MapMutation(await queueService.CheckInAsync(request, cancellationToken), created: true);
    }

    [HttpPut("move")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Queue.Move)]
    public async Task<ActionResult<BaseResponse<QueueEntryResponse>>> Move(
        [FromBody] QueueMoveRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await moveValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<QueueEntryResponse> { Status = false, Error = "validation_error" });

        return MapMutation(await queueService.MoveAsync(request, cancellationToken));
    }

    private ActionResult<BaseResponse<QueueEntryResponse>> MapMutation(
        QueueMutationResult result,
        bool created = false)
    {
        return result.Outcome switch
        {
            QueueMutationOutcome.Success when created => StatusCode(
                StatusCodes.Status201Created,
                new BaseResponse<QueueEntryResponse> { Status = true, Data = result.Entry }),
            QueueMutationOutcome.Success => Ok(new BaseResponse<QueueEntryResponse>
            {
                Status = true,
                Data = result.Entry
            }),
            QueueMutationOutcome.NotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Queue resource not found."
            }),
            QueueMutationOutcome.Conflict => Conflict(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Queue state changed.",
                Error = "queue_conflict"
            }),
            QueueMutationOutcome.ConfigurationRequired => Conflict(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Queue workflow configuration is required.",
                Error = "queue_configuration_required"
            }),
            QueueMutationOutcome.ValidationError => BadRequest(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Queue operation is invalid.",
                Error = "validation_error"
            }),
            QueueMutationOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to process queue operation."
            })
        };
    }
}
