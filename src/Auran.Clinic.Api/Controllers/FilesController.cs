using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Files;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Infrastructure.Files;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/files")]
[Produces("application/json")]
public sealed class FilesController(
    IFileAttachmentService fileAttachmentService,
    IValidator<PatientAttachmentLookupRequest> patientLookupValidator,
    IValidator<FileDownloadRequest> downloadValidator,
    IValidator<PatientAttachmentUploadMetadata> patientUploadValidator,
    IValidator<ClinicalOrderAttachmentUploadMetadata> clinicalOrderUploadValidator,
    IOptions<FileStorageOptions> storageOptions) : ControllerBase
{
    [HttpPost("patient/list")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Files.View)]
    public async Task<ActionResult<BaseResponse<IReadOnlyCollection<FileAttachmentResponse>>>> PatientFiles(
        [FromBody] PatientAttachmentLookupRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await patientLookupValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<IReadOnlyCollection<FileAttachmentResponse>> { Status = false, Error = "validation_error" });

        var result = await fileAttachmentService.ListPatientFilesAsync(request.PatientId, cancellationToken);
        if (result is null)
            return NotFound(new BaseResponse { Status = false, Message = "Patient not found." });

        return Ok(new BaseResponse<IReadOnlyCollection<FileAttachmentResponse>> { Status = true, Data = result });
    }

    [HttpPost("patient/upload")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Files.Upload)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<BaseResponse<FileAttachmentResponse>>> UploadPatientFile(
        [FromForm] PatientFileUploadForm request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length <= 0 || request.File.Length > storageOptions.Value.MaxFileSizeBytes)
            return BadRequest(new BaseResponse<FileAttachmentResponse> { Status = false, Error = "invalid_file" });

        var metadata = new PatientAttachmentUploadMetadata
        {
            PatientId = request.PatientId,
            Category = request.Category,
            Notes = request.Notes
        };

        var validation = await patientUploadValidator.ValidateAsync(metadata, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<FileAttachmentResponse> { Status = false, Error = "validation_error" });

        await using var stream = request.File.OpenReadStream();
        var result = await fileAttachmentService.UploadPatientFileAsync(
            metadata,
            stream,
            request.File.FileName,
            request.File.ContentType,
            request.File.Length,
            cancellationToken);

        if (result is null)
            return BadRequest(new BaseResponse<FileAttachmentResponse>
            {
                Status = false,
                Message = "Patient not found or file type/size is not allowed.",
                Error = "file_upload_rejected"
            });

        return StatusCode(StatusCodes.Status201Created, new BaseResponse<FileAttachmentResponse>
        {
            Status = true,
            Data = result
        });
    }

    [HttpPost("clinical-order/list")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Files.View)]
    public async Task<ActionResult<BaseResponse<IReadOnlyCollection<FileAttachmentResponse>>>> ClinicalOrderFiles(
        [FromBody] ClinicalOrderFileListRequest request,
        CancellationToken cancellationToken)
    {
        if (request.VisitId == Guid.Empty)
            return BadRequest(new BaseResponse<IReadOnlyCollection<FileAttachmentResponse>> { Status = false, Error = "validation_error" });

        var result = await fileAttachmentService.ListClinicalOrderFilesAsync(request.VisitId, cancellationToken);
        if (result is null)
            return NotFound(new BaseResponse { Status = false, Message = "Clinical order not found." });

        return Ok(new BaseResponse<IReadOnlyCollection<FileAttachmentResponse>> { Status = true, Data = result });
    }

    [HttpPost("clinical-order/upload")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Files.Upload)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<BaseResponse<FileAttachmentResponse>>> UploadClinicalOrderFile(
        [FromForm] ClinicalOrderFileUploadForm request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length <= 0 || request.File.Length > storageOptions.Value.MaxFileSizeBytes)
            return BadRequest(new BaseResponse<FileAttachmentResponse> { Status = false, Error = "invalid_file" });

        var metadata = new ClinicalOrderAttachmentUploadMetadata
        {
            VisitId = request.VisitId,
            DefinitionCode = request.DefinitionCode
        };

        var validation = await clinicalOrderUploadValidator.ValidateAsync(metadata, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<FileAttachmentResponse> { Status = false, Error = "validation_error" });

        await using var stream = request.File.OpenReadStream();
        var result = await fileAttachmentService.UploadClinicalOrderFileAsync(
            metadata,
            stream,
            request.File.FileName,
            request.File.ContentType,
            request.File.Length,
            cancellationToken);

        if (result is null)
            return BadRequest(new BaseResponse<FileAttachmentResponse>
            {
                Status = false,
                Message = "Clinical order/section not found or file type/size is not allowed.",
                Error = "file_upload_rejected"
            });

        return StatusCode(StatusCodes.Status201Created, new BaseResponse<FileAttachmentResponse>
        {
            Status = true,
            Data = result
        });
    }

    [HttpPost("download")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Files.View)]
    public async Task<IActionResult> Download(
        [FromBody] FileDownloadRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await downloadValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse { Status = false, Error = "validation_error" });

        var file = await fileAttachmentService.OpenFileAsync(request.FileId, cancellationToken);
        if (file is null)
            return NotFound(new BaseResponse { Status = false, Message = "File not found." });

        return File(file.Content, file.ContentType, file.DownloadName);
    }
}

public sealed class PatientFileUploadForm
{
    public Guid PatientId { get; init; }
    public string? Category { get; init; }
    public string? Notes { get; init; }
    public IFormFile? File { get; init; }
}

public sealed class ClinicalOrderFileUploadForm
{
    public Guid VisitId { get; init; }
    public string? DefinitionCode { get; init; }
    public IFormFile? File { get; init; }
}

public sealed class ClinicalOrderFileListRequest
{
    public Guid VisitId { get; init; }
}
