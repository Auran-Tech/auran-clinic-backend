using FluentValidation;

namespace Auran.Clinic.Application.Files;

public sealed record FileAttachmentResponse(
    Guid FileId,
    string OriginalName,
    string ContentType,
    long Size,
    DateTime UploadedAtUtc,
    string? Category,
    string? Notes);

public sealed class PatientAttachmentLookupRequest
{
    public Guid PatientId { get; init; }
}

public sealed class FileDownloadRequest
{
    public Guid FileId { get; init; }
}

public sealed class PatientAttachmentUploadMetadata
{
    public Guid PatientId { get; init; }
    public string? Category { get; init; }
    public string? Notes { get; init; }
}

public sealed class ClinicalOrderAttachmentUploadMetadata
{
    public Guid VisitId { get; init; }
    public string? DefinitionCode { get; init; }
}

public sealed class PatientAttachmentLookupRequestValidator : AbstractValidator<PatientAttachmentLookupRequest>
{
    public PatientAttachmentLookupRequestValidator() => RuleFor(x => x.PatientId).NotEmpty();
}

public sealed class FileDownloadRequestValidator : AbstractValidator<FileDownloadRequest>
{
    public FileDownloadRequestValidator() => RuleFor(x => x.FileId).NotEmpty();
}

public sealed class PatientAttachmentUploadMetadataValidator : AbstractValidator<PatientAttachmentUploadMetadata>
{
    public PatientAttachmentUploadMetadataValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Category).MaximumLength(128);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public sealed class ClinicalOrderAttachmentUploadMetadataValidator : AbstractValidator<ClinicalOrderAttachmentUploadMetadata>
{
    public ClinicalOrderAttachmentUploadMetadataValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.DefinitionCode).MaximumLength(64);
    }
}
