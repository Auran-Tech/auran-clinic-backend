using Auran.Clinic.Domain.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.Infrastructure.Files;

public sealed class LocalFileStorageProvider(
    IHostEnvironment environment,
    IOptions<FileStorageOptions> options) : IFileStorageProvider
{
    private readonly string rootPath = ResolveRootPath(environment.ContentRootPath, options.Value.LocalRootPath);

    public FileStorageProvider Provider => FileStorageProvider.Local;

    public async Task SaveAsync(
        string storageKey,
        Stream content,
        long maxBytes,
        CancellationToken cancellationToken = default)
    {
        var finalPath = ResolveStoragePath(storageKey);
        var directory = Path.GetDirectoryName(finalPath)
            ?? throw new InvalidOperationException("Unable to resolve the file storage directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = $"{finalPath}.uploading-{Guid.NewGuid():N}";
        try
        {
            await using var output = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var buffer = new byte[81920];
            long totalBytes = 0;
            while (true)
            {
                var read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read == 0)
                    break;

                totalBytes += read;
                if (totalBytes > maxBytes)
                    throw new InvalidOperationException("Uploaded content exceeds the allowed file size.");

                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            await output.FlushAsync(cancellationToken);
            File.Move(temporaryPath, finalPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
            throw;
        }
    }

    public Task<long?> GetSizeAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolveStoragePath(storageKey);
        long? result = File.Exists(path) ? new FileInfo(path).Length : null;
        return Task.FromResult(result);
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolveStoragePath(storageKey);
        if (!File.Exists(path))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteIfExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolveStoragePath(storageKey);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    private string ResolveStoragePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key is required.", nameof(storageKey));

        var normalizedKey = storageKey.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, normalizedKey));
        var rootPrefix = rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? rootPath
            : rootPath + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid file storage key.");

        return fullPath;
    }

    private static string ResolveRootPath(string contentRootPath, string configuredPath)
    {
        var value = string.IsNullOrWhiteSpace(configuredPath) ? "storage" : configuredPath.Trim();
        return Path.GetFullPath(Path.IsPathRooted(value) ? value : Path.Combine(contentRootPath, value));
    }
}
