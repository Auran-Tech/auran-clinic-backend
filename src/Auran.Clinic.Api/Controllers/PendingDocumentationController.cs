using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.PendingDocumentation;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/pending-documentation")]
[Produces("application/json")]
public sealed class PendingDocumentationController(
    IPendingDocumentationService pendingDocumentationService,
    IValidator<CompletePendingDocumentationRequest> completeValidator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.View)]
    [SwaggerOperation(
        Summary = "List pending clinical documentation",
        Description = "Returns Draft/Pending visits. mineOnly defaults to true for normal users; Clinic Super Users may view all.",
        OperationId = "PendingDocumentation_List",
        Tags = new[] { "Clinical Documentation" })]
    public async Task<ActionResult<BaseResponse<IReadOnlyList<PendingDocumentationResponse>>>> List(
        [FromQuery] PendingDocumentationQuery query,
        CancellationToken cancellationToken)
    {
        var data = await pendingDocumentationService.ListAsync(query, cancellationToken);
        return Ok(new BaseResponse<IReadOnlyList<PendingDocumentationResponse>>
        {
            Status = true,
            Data = data
        });
    }

    [HttpPut("complete")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.Edit)]
    [SwaggerOperation(
        Summary = "Complete pending clinical documentation",
        Description = "Allows the assigned doctor or Clinic Super User to finish documentation after the operational visit/session has ended.",
        OperationId = "PendingDocumentation_Complete",
        Tags = new[] { "Clinical Documentation" })]
    public async Task<ActionResult<BaseResponse<PendingDocumentationResponse>>> Complete(
        [FromBody] CompletePendingDocumentationRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await completeValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new BaseResponse<PendingDocumentationResponse>
            {
                Status = false,
                Error = "validation_error"
            });
        }

        var result = await pendingDocumentationService.CompleteAsync(request, cancellationToken);

        return result.Outcome switch
        {
            PendingDocumentationOutcome.Success => Ok(new BaseResponse<PendingDocumentationResponse>
            {
                Status = true,
                Data = result.Documentation
            }),
            PendingDocumentationOutcome.VisitNotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Visit not found."
            }),
            PendingDocumentationOutcome.Forbidden => StatusCode(
                StatusCodes.Status403Forbidden,
                new BaseResponse
                {
                    Status = false,
                    Message = "Only the assigned doctor or a Clinic Super User can complete this documentation."
                }),
            PendingDocumentationOutcome.NotPending => Conflict(new BaseResponse
            {
                Status = false,
                Message = "The visit documentation is no longer pending.",
                Error = "documentation_not_pending"
            }),
            PendingDocumentationOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to complete documentation."
            })
        };
    }
}
