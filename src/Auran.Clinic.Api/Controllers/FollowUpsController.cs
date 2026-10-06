using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.FollowUps;
using Auran.Clinic.Application.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/follow-ups")]
[Produces("application/json")]
public sealed class FollowUpsController(
    IFollowUpService followUpService,
    IValidator<CreateFollowUpRequest> createValidator,
    IValidator<UpdateFollowUpRequest> updateValidator,
    IValidator<SetFollowUpStatusRequest> statusValidator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.FollowUps.View)]
    public async Task<ActionResult<BaseResponse<IReadOnlyCollection<FollowUpResponse>>>> List(
        [FromQuery] string? search = null,
        [FromQuery] string? dueCategory = null,
        CancellationToken cancellationToken = default)
    {
        var result = await followUpService.ListAsync(search, dueCategory, cancellationToken);
        return Ok(new BaseResponse<IReadOnlyCollection<FollowUpResponse>>
        {
            Status = true,
            Data = result
        });
    }

    [HttpPost]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.FollowUps.Manage)]
    public async Task<ActionResult<BaseResponse<FollowUpResponse>>> Create(
        [FromBody] CreateFollowUpRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<FollowUpResponse> { Status = false, Error = "validation_error" });

        return MapMutation(await followUpService.CreateAsync(request, cancellationToken), created: true);
    }

    [HttpPut]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.FollowUps.Manage)]
    public async Task<ActionResult<BaseResponse<FollowUpResponse>>> Update(
        [FromBody] UpdateFollowUpRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<FollowUpResponse> { Status = false, Error = "validation_error" });

        return MapMutation(await followUpService.UpdateAsync(request, cancellationToken));
    }

    [HttpPut("status")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.FollowUps.Manage)]
    public async Task<ActionResult<BaseResponse<FollowUpResponse>>> SetStatus(
        [FromBody] SetFollowUpStatusRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await statusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<FollowUpResponse> { Status = false, Error = "validation_error" });

        return MapMutation(await followUpService.SetStatusAsync(request, cancellationToken));
    }

    private ActionResult<BaseResponse<FollowUpResponse>> MapMutation(
        FollowUpMutationResult result,
        bool created = false)
    {
        return result.Outcome switch
        {
            FollowUpMutationOutcome.Success when created => StatusCode(
                StatusCodes.Status201Created,
                new BaseResponse<FollowUpResponse> { Status = true, Data = result.FollowUp }),
            FollowUpMutationOutcome.Success => Ok(new BaseResponse<FollowUpResponse>
            {
                Status = true,
                Data = result.FollowUp
            }),
            FollowUpMutationOutcome.NotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Follow-up resource not found."
            }),
            FollowUpMutationOutcome.ValidationError => BadRequest(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Follow-up operation is invalid.",
                Error = "validation_error"
            }),
            FollowUpMutationOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to process follow-up operation."
            })
        };
    }
}
