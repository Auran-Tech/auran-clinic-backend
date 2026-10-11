namespace Auran.Clinic.Application.Attachments;

public sealed record PatientAttachmentResponse(
    Guid AttachmentId,
    Guid FileId,
    Guid PatientId,
    string OriginalName,
    string ContentType,
    long Size,
    string? Category,
    string? Notes,
    DateTime UploadedAtUtc);

public sealed record PatientAttachmentDownload(
    Stream Content,
    string ContentType,
    string OriginalName);

public enum PatientAttachmentOutcome
{
    Success,
    PatientNotFound,
    AttachmentNotFound,
    ValidationError,
    Unauthenticated
}

public sealed record PatientAttachmentResult(
    PatientAttachmentOutcome Outcome,
    PatientAttachmentResponse? Attachment = null,
    string? Error = null);
