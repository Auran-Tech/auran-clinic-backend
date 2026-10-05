namespace Auran.Clinic.Application.Files;

public sealed record StoredFileResult(
    string Provider,
    string StorageKey,
    string StoredName);

public sealed record StoredFileContent(
    Stream Content,
    string ContentType,
    string DownloadName);

public interface IFileStorage
{
    Task<StoredFileResult> SaveAsync(
        Stream content,
        string originalName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}
