namespace Auran.Clinic.Application.ClinicalOrderAttachments;

public sealed record ClinicalOrderAttachmentSectionResponse(
    Guid SectionDefinitionId,
    string Name,
    string SectionType,
    int SortOrder);

public sealed record ClinicalOrderAttachmentFileResponse(
    Guid AttachmentId,
    Guid FileId,
    string OriginalName,
    string ContentType,
    long Size,
    string? Category,
    string? Notes,
    DateTime UploadedAtUtc);

public sealed record ClinicalOrderAttachmentLinkResponse(
    Guid LinkId,
    Guid FileId,
    Guid SectionDefinitionId,
    string SectionName,
    string SectionType,
    string OriginalName,
    string ContentType,
    long Size);

public sealed record ClinicalOrderAttachmentWorkspaceResponse(
    Guid VisitId,
    Guid PatientId,
    Guid? ClinicalOrderId,
    IReadOnlyList<ClinicalOrderAttachmentSectionResponse> Sections,
    IReadOnlyList<ClinicalOrderAttachmentFileResponse> Files,
    IReadOnlyList<ClinicalOrderAttachmentLinkResponse> Links);

public sealed class LinkClinicalOrderAttachmentRequest
{
    public Guid VisitId { get; init; }
    public Guid FileId { get; init; }
    public Guid SectionDefinitionId { get; init; }
}

public sealed class DeleteClinicalOrderAttachmentRequest
{
    public Guid LinkId { get; init; }
}

public enum ClinicalOrderAttachmentOutcome
{
    Success,
    VisitNotFound,
    VisitClosed,
    FileNotFound,
    InvalidSection,
    Forbidden,
    Conflict,
    Unauthenticated
}

public sealed record ClinicalOrderAttachmentResult(
    ClinicalOrderAttachmentOutcome Outcome,
    ClinicalOrderAttachmentWorkspaceResponse? Workspace = null,
    string? Error = null);
