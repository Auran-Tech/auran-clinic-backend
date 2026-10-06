using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Visits;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/visits")]
[Produces("application/json")]
public sealed class VisitsController(
    IVisitService visitService,
    IValidator<StartVisitRequest> startValidator) : ControllerBase
{
    [HttpPost("start")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.Start)]
    [SwaggerOperation(
        Summary = "Check a patient into the clinic",
        Description = "Creates an open visit and its initial queue entry using the first configured workflow status for the authenticated clinic.",
        OperationId = "Visits_Start",
        Tags = new[] { "Visits", "Queue" })]
    public async Task<ActionResult<BaseResponse<VisitResponse>>> Start(
        [FromBody] StartVisitRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await startValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new BaseResponse<VisitResponse>
            {
                Status = false,
                Error = "validation_error"
            });
        }

        var result = await visitService.StartAsync(request, cancellationToken);
        return result.Outcome switch
        {
            VisitStartOutcome.Success => StatusCode(
                StatusCodes.Status201Created,
                new BaseResponse<VisitResponse> { Status = true, Data = result.Visit }),
            VisitStartOutcome.PatientNotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Patient not found."
            }),
            VisitStartOutcome.DoctorNotFound => BadRequest(new BaseResponse
            {
                Status = false,
                Message = "Selected doctor was not found or is inactive.",
                Error = "doctor_not_found"
            }),
            VisitStartOutcome.ActiveVisitExists => Conflict(new BaseResponse
            {
                Status = false,
                Message = "The patient already has an active visit.",
                Error = "active_visit_exists"
            }),
            VisitStartOutcome.WorkflowNotConfigured => Conflict(new BaseResponse
            {
                Status = false,
                Message = "Clinic workflow is not configured.",
                Error = "workflow_not_configured"
            }),
            VisitStartOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to start the visit."
            })
        };
    }

    [HttpGet("active")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.View)]
    [SwaggerOperation(
        Summary = "Get a patient's active visit",
        Description = "Returns the patient's open visit and current queue workflow status in the authenticated clinic.",
        OperationId = "Visits_GetActiveForPatient",
        Tags = new[] { "Visits", "Queue" })]
    public async Task<ActionResult<BaseResponse<VisitResponse>>> GetActiveForPatient(
        [FromQuery] Guid patientId,
        CancellationToken cancellationToken)
    {
        if (patientId == Guid.Empty)
        {
            return BadRequest(new BaseResponse<VisitResponse>
            {
                Status = false,
                Error = "validation_error"
            });
        }

        var visit = await visitService.GetActiveForPatientAsync(patientId, cancellationToken);
        if (visit is null)
        {
            return NotFound(new BaseResponse
            {
                Status = false,
                Message = "Active visit not found."
            });
        }

        return Ok(new BaseResponse<VisitResponse>
        {
            Status = true,
            Data = visit
        });
    }
}
