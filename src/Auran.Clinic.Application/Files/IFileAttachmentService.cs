namespace Auran.Clinic.Application.Files;

public interface IFileAttachmentService
{
    Task<IReadOnlyCollection<FileAttachmentResponse>?> ListPatientFilesAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<FileAttachmentResponse?> UploadPatientFileAsync(
        PatientAttachmentUploadMetadata metadata,
        Stream content,
        string originalName,
        string contentType,
        long size,
        CancellationToken cancellationToken = default);

    Task<FileAttachmentResponse?> UploadClinicalOrderFileAsync(
        ClinicalOrderAttachmentUploadMetadata metadata,
        Stream content,
        string originalName,
        string contentType,
        long size,
        CancellationToken cancellationToken = default);

    Task<StoredFileContent?> OpenFileAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);
}
