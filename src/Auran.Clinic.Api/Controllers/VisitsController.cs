using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Visits;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/visits")]
[Produces("application/json")]
public sealed class VisitsController(
    IVisitService visitService,
    IValidator<VisitLookupRequest> lookupValidator,
    IValidator<StartVisitSessionRequest> startSessionValidator,
    IValidator<EndVisitSessionRequest> endSessionValidator,
    IValidator<SaveVisitDraftRequest> saveDraftValidator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.View)]
    public async Task<ActionResult<BaseResponse<IReadOnlyCollection<VisitSummaryResponse>>>> List(
        CancellationToken cancellationToken)
    {
        var visits = await visitService.ListAsync(cancellationToken);
        return Ok(new BaseResponse<IReadOnlyCollection<VisitSummaryResponse>>
        {
            Status = true,
            Data = visits
        });
    }

    [HttpPost("details")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.View)]
    public async Task<ActionResult<BaseResponse<VisitDetailsResponse>>> Get(
        [FromBody] VisitLookupRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await lookupValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<VisitDetailsResponse> { Status = false, Error = "validation_error" });

        var visit = await visitService.GetAsync(request.VisitId, cancellationToken);
        if (visit is null)
            return NotFound(new BaseResponse { Status = false, Message = "Visit not found." });

        return Ok(new BaseResponse<VisitDetailsResponse> { Status = true, Data = visit });
    }

    [HttpPost("sessions/start")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.Start)]
    public async Task<ActionResult<BaseResponse<VisitDetailsResponse>>> StartSession(
        [FromBody] StartVisitSessionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await startSessionValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<VisitDetailsResponse> { Status = false, Error = "validation_error" });

        return MapMutation(await visitService.StartSessionAsync(request, cancellationToken));
    }

    [HttpPut("sessions/end")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.Start)]
    public async Task<ActionResult<BaseResponse<VisitDetailsResponse>>> EndSession(
        [FromBody] EndVisitSessionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await endSessionValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<VisitDetailsResponse> { Status = false, Error = "validation_error" });

        return MapMutation(await visitService.EndSessionAsync(request, cancellationToken));
    }

    [HttpPut("draft")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.Edit)]
    public async Task<ActionResult<BaseResponse<VisitDetailsResponse>>> SaveDraft(
        [FromBody] SaveVisitDraftRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await saveDraftValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<VisitDetailsResponse> { Status = false, Error = "validation_error" });

        return MapMutation(await visitService.SaveDraftAsync(request, cancellationToken));
    }

    private ActionResult<BaseResponse<VisitDetailsResponse>> MapMutation(VisitMutationResult result)
    {
        return result.Outcome switch
        {
            VisitMutationOutcome.Success => Ok(new BaseResponse<VisitDetailsResponse>
            {
                Status = true,
                Data = result.Visit
            }),
            VisitMutationOutcome.NotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Visit resource not found."
            }),
            VisitMutationOutcome.Conflict => Conflict(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Visit changed by another user.",
                Error = "visit_conflict"
            }),
            VisitMutationOutcome.ValidationError => BadRequest(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Visit operation is invalid.",
                Error = "validation_error"
            }),
            VisitMutationOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to process visit operation."
            })
        };
    }
}
