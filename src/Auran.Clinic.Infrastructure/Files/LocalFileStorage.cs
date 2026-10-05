using Auran.Clinic.Application.Files;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.Infrastructure.Files;

public sealed class LocalFileStorage(
    IHostEnvironment environment,
    IOptions<FileStorageOptions> options) : IFileStorage
{
    private readonly FileStorageOptions _options = options.Value;

    public async Task<StoredFileResult> SaveAsync(
        Stream content,
        string originalName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalName);
        if (extension.Length > 16)
            extension = string.Empty;

        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var datePrefix = DateTime.UtcNow.ToString("yyyy/MM");
        var storageKey = $"{datePrefix}/{storedName}".Replace('\\', '/');
        var fullPath = ResolvePath(storageKey);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var output = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true);

        await content.CopyToAsync(output, cancellationToken);
        return new StoredFileResult("Local", storageKey, storedName);
    }

    public Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(storageKey);
        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(storageKey);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        var root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, _options.LocalRootPath));
        var relative = storageKey.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(root, relative));

        if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid storage key.");

        return fullPath;
    }
}
