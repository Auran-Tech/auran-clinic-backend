namespace Auran.Clinic.Application.Attachments;

public interface IPatientAttachmentService
{
    Task<IReadOnlyList<PatientAttachmentResponse>> ListAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<PatientAttachmentResult> UploadAsync(
        Guid patientId,
        string originalName,
        string contentType,
        long size,
        Stream content,
        string? category,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<PatientAttachmentDownload?> DownloadAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default);
}
