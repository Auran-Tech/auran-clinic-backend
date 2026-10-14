using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.ClinicalSessions;
using Auran.Clinic.Application.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/clinical-sessions")]
[Produces("application/json")]
public sealed class ClinicalSessionsController(
    IClinicalSessionService clinicalSessionService,
    IValidator<StartClinicalSessionRequest> startValidator,
    IValidator<SaveClinicalDocumentationRequest> saveValidator,
    IValidator<EndClinicalSessionRequest> endValidator) : ControllerBase
{
    [HttpGet("active")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.View)]
    [SwaggerOperation(
        Summary = "Get the active clinical session",
        Description = "Returns the open clinical session and current visit documentation for the authenticated clinic.",
        OperationId = "ClinicalSessions_GetActive",
        Tags = new[] { "Clinical Sessions" })]
    public async Task<ActionResult<BaseResponse<ClinicalSessionResponse>>> GetActive(
        [FromQuery] Guid visitId,
        CancellationToken cancellationToken)
    {
        if (visitId == Guid.Empty)
        {
            return BadRequest(new BaseResponse<ClinicalSessionResponse>
            {
                Status = false,
                Error = "validation_error"
            });
        }

        var session = await clinicalSessionService.GetActiveAsync(visitId, cancellationToken);
        if (session is null)
        {
            return NotFound(new BaseResponse
            {
                Status = false,
                Message = "Active clinical session not found."
            });
        }

        return Ok(new BaseResponse<ClinicalSessionResponse>
        {
            Status = true,
            Data = session
        });
    }

    [HttpPost("start")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.Edit)]
    [SwaggerOperation(
        Summary = "Start a clinical session",
        Description = "Starts one active clinical session for an open visit. The assigned doctor or a Clinic Super User may start the session.",
        OperationId = "ClinicalSessions_Start",
        Tags = new[] { "Clinical Sessions" })]
    public async Task<ActionResult<BaseResponse<ClinicalSessionResponse>>> Start(
        [FromBody] StartClinicalSessionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await startValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailure();

        return Map(await clinicalSessionService.StartAsync(request, cancellationToken), created: true);
    }

    [HttpPut("documentation")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.Edit)]
    [SwaggerOperation(
        Summary = "Save visit clinical documentation",
        Description = "Saves draft clinical documentation while an active clinical session exists.",
        OperationId = "ClinicalSessions_SaveDocumentation",
        Tags = new[] { "Clinical Sessions" })]
    public async Task<ActionResult<BaseResponse<ClinicalSessionResponse>>> SaveDocumentation(
        [FromBody] SaveClinicalDocumentationRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await saveValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailure();

        return Map(await clinicalSessionService.SaveDocumentationAsync(request, cancellationToken));
    }

    [HttpPut("end")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.Edit)]
    [SwaggerOperation(
        Summary = "End a clinical session",
        Description = "Ends the active clinical session and marks documentation Completed when clinical content exists, otherwise Pending.",
        OperationId = "ClinicalSessions_End",
        Tags = new[] { "Clinical Sessions" })]
    public async Task<ActionResult<BaseResponse<ClinicalSessionResponse>>> End(
        [FromBody] EndClinicalSessionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await endValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailure();

        return Map(await clinicalSessionService.EndAsync(request, cancellationToken));
    }

    private ActionResult<BaseResponse<ClinicalSessionResponse>> Map(
        ClinicalSessionResult result,
        bool created = false)
    {
        return result.Outcome switch
        {
            ClinicalSessionOutcome.Success when created => StatusCode(
                StatusCodes.Status201Created,
                new BaseResponse<ClinicalSessionResponse> { Status = true, Data = result.Session }),
            ClinicalSessionOutcome.Success => Ok(new BaseResponse<ClinicalSessionResponse>
            {
                Status = true,
                Data = result.Session
            }),
            ClinicalSessionOutcome.VisitNotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Visit not found."
            }),
            ClinicalSessionOutcome.SessionNotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Active clinical session not found."
            }),
            ClinicalSessionOutcome.Forbidden => StatusCode(
                StatusCodes.Status403Forbidden,
                new BaseResponse
                {
                    Status = false,
                    Message = "Only the assigned doctor or a Clinic Super User can manage this clinical session."
                }),
            ClinicalSessionOutcome.SessionAlreadyActive => Conflict(new BaseResponse
            {
                Status = false,
                Message = "An active clinical session already exists.",
                Error = "clinical_session_active"
            }),
            ClinicalSessionOutcome.VisitClosed => Conflict(new BaseResponse
            {
                Status = false,
                Message = "The visit is no longer open.",
                Error = "visit_closed"
            }),
            ClinicalSessionOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to manage the clinical session."
            })
        };
    }

    private BadRequestObjectResult ValidationFailure() =>
        BadRequest(new BaseResponse<ClinicalSessionResponse>
        {
            Status = false,
            Error = "validation_error"
        });
}
