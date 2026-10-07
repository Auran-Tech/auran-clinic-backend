using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.ClinicalOrderAttachments;
using Auran.Clinic.Application.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/clinical-order-attachments")]
[Produces("application/json")]
public sealed class ClinicalOrderAttachmentsController(
    IClinicalOrderAttachmentService service,
    IValidator<LinkClinicalOrderAttachmentRequest> linkValidator,
    IValidator<DeleteClinicalOrderAttachmentRequest> deleteValidator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.View)]
    [SwaggerOperation(
        Summary = "Get clinical order attachment workspace",
        Description = "Returns enabled Image/File order sections, eligible patient attachments, and current order links for a visit.",
        OperationId = "ClinicalOrderAttachments_GetWorkspace",
        Tags = new[] { "Clinical Orders", "Files" })]
    public async Task<ActionResult<BaseResponse<ClinicalOrderAttachmentWorkspaceResponse>>> Get(
        [FromQuery] Guid visitId,
        CancellationToken cancellationToken)
    {
        if (visitId == Guid.Empty)
        {
            return BadRequest(new BaseResponse<ClinicalOrderAttachmentWorkspaceResponse>
            {
                Status = false,
                Error = "validation_error"
            });
        }

        var workspace = await service.GetWorkspaceAsync(visitId, cancellationToken);
        if (workspace is null)
        {
            return NotFound(new BaseResponse
            {
                Status = false,
                Message = "Visit not found."
            });
        }

        return Ok(new BaseResponse<ClinicalOrderAttachmentWorkspaceResponse>
        {
            Status = true,
            Data = workspace
        });
    }

    [HttpPost]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.Edit)]
    [SwaggerOperation(
        Summary = "Link a patient file to a clinical order section",
        Description = "Links an existing patient attachment into an enabled Image/File clinical-order section. Assigned doctor or Clinic Super User only.",
        OperationId = "ClinicalOrderAttachments_Link",
        Tags = new[] { "Clinical Orders", "Files" })]
    public async Task<ActionResult<BaseResponse<ClinicalOrderAttachmentWorkspaceResponse>>> Link(
        [FromBody] LinkClinicalOrderAttachmentRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await linkValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure();

        var result = await service.LinkAsync(request, cancellationToken);
        return Map(result, created: true);
    }

    [HttpDelete]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.Edit)]
    [SwaggerOperation(
        Summary = "Unlink a clinical order attachment",
        Description = "Removes only the clinical-order link; the underlying patient file remains stored and attached to the patient.",
        OperationId = "ClinicalOrderAttachments_Unlink",
        Tags = new[] { "Clinical Orders", "Files" })]
    public async Task<ActionResult<BaseResponse<ClinicalOrderAttachmentWorkspaceResponse>>> Delete(
        [FromBody] DeleteClinicalOrderAttachmentRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await deleteValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure();

        return Map(await service.DeleteAsync(request, cancellationToken));
    }

    private ActionResult<BaseResponse<ClinicalOrderAttachmentWorkspaceResponse>> Map(
        ClinicalOrderAttachmentResult result,
        bool created = false)
    {
        return result.Outcome switch
        {
            ClinicalOrderAttachmentOutcome.Success when created => StatusCode(
                StatusCodes.Status201Created,
                new BaseResponse<ClinicalOrderAttachmentWorkspaceResponse>
                {
                    Status = true,
                    Data = result.Workspace
                }),
            ClinicalOrderAttachmentOutcome.Success => Ok(
                new BaseResponse<ClinicalOrderAttachmentWorkspaceResponse>
                {
                    Status = true,
                    Data = result.Workspace
                }),
            ClinicalOrderAttachmentOutcome.VisitNotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Visit not found."
            }),
            ClinicalOrderAttachmentOutcome.VisitClosed => Conflict(new BaseResponse
            {
                Status = false,
                Message = "Clinical order attachments can only be changed for an open visit.",
                Error = "visit_closed"
            }),
            ClinicalOrderAttachmentOutcome.FileNotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Patient attachment not found."
            }),
            ClinicalOrderAttachmentOutcome.LinkNotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Clinical order attachment link not found."
            }),
            ClinicalOrderAttachmentOutcome.InvalidSection => BadRequest(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Invalid clinical order attachment section.",
                Error = "invalid_order_attachment_section"
            }),
            ClinicalOrderAttachmentOutcome.Forbidden => StatusCode(
                StatusCodes.Status403Forbidden,
                new BaseResponse
                {
                    Status = false,
                    Message = "Only the assigned doctor or a Clinic Super User can manage clinical order attachments."
                }),
            ClinicalOrderAttachmentOutcome.Conflict => Conflict(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Clinical order attachment already exists.",
                Error = "clinical_order_attachment_conflict"
            }),
            ClinicalOrderAttachmentOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to manage clinical order attachments."
            })
        };
    }

    private BadRequestObjectResult ValidationFailure() =>
        BadRequest(new BaseResponse<ClinicalOrderAttachmentWorkspaceResponse>
        {
            Status = false,
            Error = "validation_error"
        });
}
