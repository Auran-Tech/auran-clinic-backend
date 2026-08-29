using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Files;
using Auran.Clinic.Application.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Route("api/files")]
[Authorize(Policy = ActorPolicies.Clinic)]
public sealed class FilesController(
    IFileService fileService,
    IValidator<CreateFileUploadSessionRequest> createValidator) : ControllerBase
{
    [HttpPost("upload-sessions")]
    [Authorize(Policy = PermissionPolicy.ClinicPrefix + Permissions.Clinic.Files.Upload)]
    [SwaggerOperation(
        Summary = "Create a temporary file upload session",
        Description = "Creates a short-lived clinic-scoped upload session. The frontend uploads the raw file bytes to the returned UploadUrl, then calls the complete endpoint to receive the permanent FileId and file metadata. Business endpoints should store FileId rather than storage URLs.",
        OperationId = "Files_CreateUploadSession",
        Tags = new[] { "Files" })]
    [ProducesResponseType(typeof(BaseResponse<FileUploadSessionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseResponse<FileUploadSessionResponse>>> CreateUploadSession(
        [FromBody] CreateFileUploadSessionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new BaseResponse
            {
                Status = false,
                Message = "Validation failed.",
                Error = string.Join(" ", validation.Errors.Select(x => x.ErrorMessage))
            });
        }

        try
        {
            var result = await fileService.CreateUploadSessionAsync(request, cancellationToken);
            if (result is null)
                return Forbid();

            return StatusCode(StatusCodes.Status201Created, new BaseResponse<FileUploadSessionResponse>
            {
                Status = true,
                Message = "Upload session created.",
                Data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new BaseResponse { Status = false, Message = ex.Message, Error = ex.Message });
        }
    }

    [HttpPut("upload-sessions/{id:guid}/content")]
    [AllowAnonymous]
    [RequestSizeLimit(104_857_600)]
    [SwaggerOperation(
        Summary = "Upload file content to a temporary session",
        Description = "Uploads raw binary content using the short-lived token embedded in the UploadUrl. This endpoint intentionally does not require a JWT because the temporary upload token is the scoped credential. For local storage the API receives the bytes; an S3 provider can later return a presigned direct-storage URL without changing the business file contract.",
        OperationId = "Files_UploadContent",
        Tags = new[] { "Files" })]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UploadContent(
        Guid id,
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        var result = await fileService.UploadContentAsync(
            id,
            token,
            Request.Body,
            Request.ContentLength,
            Request.ContentType,
            cancellationToken);

        return result.Succeeded
            ? NoContent()
            : BadRequest(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "File upload failed.",
                Error = result.Error
            });
    }

    [HttpPost("upload-sessions/{id:guid}/complete")]
    [Authorize(Policy = PermissionPolicy.ClinicPrefix + Permissions.Clinic.Files.Upload)]
    [SwaggerOperation(
        Summary = "Complete a file upload session",
        Description = "Verifies the uploaded object, creates the permanent clinic FileRecord and returns FileId, metadata and the current download URL. The operation is idempotent after successful completion.",
        OperationId = "Files_CompleteUploadSession",
        Tags = new[] { "Files" })]
    [ProducesResponseType(typeof(BaseResponse<FileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseResponse<FileResponse>>> CompleteUploadSession(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await fileService.CompleteUploadAsync(id, cancellationToken);
            return result is null
                ? NotFound(new BaseResponse { Status = false, Message = "Upload session was not found." })
                : Ok(new BaseResponse<FileResponse>
                {
                    Status = true,
                    Message = "File upload completed.",
                    Data = result
                });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new BaseResponse { Status = false, Message = ex.Message, Error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionPolicy.ClinicPrefix + Permissions.Clinic.Files.View)]
    [SwaggerOperation(
        Summary = "Get file metadata",
        Description = "Returns metadata for a permanent file in the authenticated clinic. Cross-clinic file identifiers are treated as not found.",
        OperationId = "Files_Get",
        Tags = new[] { "Files" })]
    public async Task<ActionResult<BaseResponse<FileResponse>>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await fileService.GetAsync(id, cancellationToken);
        return result is null
            ? NotFound(new BaseResponse { Status = false, Message = "File was not found." })
            : Ok(new BaseResponse<FileResponse> { Status = true, Data = result });
    }

    [HttpGet("{id:guid}/content")]
    [Authorize(Policy = PermissionPolicy.ClinicPrefix + Permissions.Clinic.Files.View)]
    [SwaggerOperation(
        Summary = "Download file content",
        Description = "Streams file content for the authenticated clinic and writes an explicit sensitive-read audit event.",
        OperationId = "Files_Download",
        Tags = new[] { "Files" })]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var result = await fileService.OpenReadAsync(id, cancellationToken);
        return result is null
            ? NotFound(new BaseResponse { Status = false, Message = "File was not found." })
            : File(result.Content, result.ContentType, result.FileName, enableRangeProcessing: true);
    }
}
