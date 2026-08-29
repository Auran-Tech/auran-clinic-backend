using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Infrastructure.Files;

public interface IFileStorageProvider
{
    FileStorageProvider Provider { get; }

    Task SaveAsync(
        string storageKey,
        Stream content,
        long maxBytes,
        CancellationToken cancellationToken = default);

    Task<long?> GetSizeAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    Task DeleteIfExistsAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}
