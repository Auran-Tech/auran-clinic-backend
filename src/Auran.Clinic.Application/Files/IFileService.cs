namespace Auran.Clinic.Application.Files;

public interface IFileService
{
    Task<FileUploadSessionResponse?> CreateUploadSessionAsync(
        CreateFileUploadSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<FileUploadContentResult> UploadContentAsync(
        Guid sessionId,
        string uploadToken,
        Stream content,
        long? contentLength,
        string? contentType,
        CancellationToken cancellationToken = default);

    Task<FileResponse?> CompleteUploadAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<FileResponse?> GetAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);

    Task<FileDownloadResponse?> OpenReadAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);
}
