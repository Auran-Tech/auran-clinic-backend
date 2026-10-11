using Auran.Clinic.Application.Attachments;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

public sealed class UploadPatientAttachmentForm
{
    public Guid PatientId { get; init; }
    public required IFormFile File { get; init; }
    public string? Category { get; init; }
    public string? Notes { get; init; }
}

public sealed class DeletePatientAttachmentRequest
{
    public Guid AttachmentId { get; init; }
}

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/patient-attachments")]
[Produces("application/json")]
public sealed class PatientAttachmentsController(
    IPatientAttachmentService attachmentService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Files.View)]
    [SwaggerOperation(
        Summary = "List patient attachments",
        Description = "Returns file metadata for attachments linked to the selected patient in the authenticated clinic.",
        OperationId = "PatientAttachments_List",
        Tags = new[] { "Files", "Patients" })]
    public async Task<ActionResult<BaseResponse<IReadOnlyList<PatientAttachmentResponse>>>> List(
        [FromQuery] Guid patientId,
        CancellationToken cancellationToken)
    {
        if (patientId == Guid.Empty)
        {
            return BadRequest(new BaseResponse<IReadOnlyList<PatientAttachmentResponse>>
            {
                Status = false,
                Error = "validation_error"
            });
        }

        var data = await attachmentService.ListAsync(patientId, cancellationToken);
        return Ok(new BaseResponse<IReadOnlyList<PatientAttachmentResponse>>
        {
            Status = true,
            Data = data
        });
    }

    [HttpPost]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Files.Upload)]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "Upload a patient attachment",
        Description = "Uploads a JPEG, PNG, WebP, or PDF file up to 10 MB and links it to the selected patient.",
        OperationId = "PatientAttachments_Upload",
        Tags = new[] { "Files", "Patients" })]
    public async Task<ActionResult<BaseResponse<PatientAttachmentResponse>>> Upload(
        [FromForm] UploadPatientAttachmentForm request,
        CancellationToken cancellationToken)
    {
        var result = await attachmentService.UploadAsync(
            request.PatientId,
            request.File.FileName,
            request.File.ContentType,
            request.File.Length,
            request.File.OpenReadStream(),
            request.Category,
            request.Notes,
            cancellationToken);

        return result.Outcome switch
        {
            PatientAttachmentOutcome.Success => StatusCode(
                StatusCodes.Status201Created,
                new BaseResponse<PatientAttachmentResponse>
                {
                    Status = true,
                    Data = result.Attachment
                }),
            PatientAttachmentOutcome.PatientNotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Patient not found."
            }),
            PatientAttachmentOutcome.ValidationError => BadRequest(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Invalid attachment.",
                Error = "attachment_validation_error"
            }),
            PatientAttachmentOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to upload attachment."
            })
        };
    }

    [HttpGet("download")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Files.View)]
    [SwaggerOperation(
        Summary = "Download a patient attachment",
        Description = "Streams a patient attachment belonging to the authenticated clinic.",
        OperationId = "PatientAttachments_Download",
        Tags = new[] { "Files", "Patients" })]
    public async Task<IActionResult> Download(
        [FromQuery] Guid fileId,
        CancellationToken cancellationToken)
    {
        if (fileId == Guid.Empty)
            return BadRequest();

        var file = await attachmentService.DownloadAsync(fileId, cancellationToken);
        if (file is null)
            return NotFound();

        return File(file.Content, file.ContentType, file.OriginalName);
    }

    [HttpDelete]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Files.Upload)]
    [SwaggerOperation(
        Summary = "Delete a patient attachment",
        Description = "Deletes the patient attachment link. The stored file is deleted only when no clinical-order reference remains.",
        OperationId = "PatientAttachments_Delete",
        Tags = new[] { "Files", "Patients" })]
    public async Task<ActionResult<BaseResponse>> Delete(
        [FromBody] DeletePatientAttachmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AttachmentId == Guid.Empty)
        {
            return BadRequest(new BaseResponse
            {
                Status = false,
                Error = "validation_error"
            });
        }

        var deleted = await attachmentService.DeleteAsync(request.AttachmentId, cancellationToken);
        if (!deleted)
        {
            return NotFound(new BaseResponse
            {
                Status = false,
                Message = "Attachment not found."
            });
        }

        return Ok(new BaseResponse
        {
            Status = true,
            Message = "Attachment deleted."
        });
    }
}
