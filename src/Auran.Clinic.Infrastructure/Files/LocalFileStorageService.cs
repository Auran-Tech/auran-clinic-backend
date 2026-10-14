using Auran.Clinic.Application.Files;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.Infrastructure.Files;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string RootPath { get; init; } = "App_Data/uploads";
}

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IOptions<FileStorageOptions> options)
    {
        var configured = options.Value.RootPath;
        _rootPath = Path.GetFullPath(
            Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(AppContext.BaseDirectory, configured));

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<StoredFileWriteResult> SaveAsync(
        Stream content,
        string originalName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        // Storage names must not inherit attacker-controlled filename extensions.
        // Only validated, supported MIME types are persisted by the attachment API.
        var extension = contentType.ToLowerInvariant() switch
        {
            "application/pdf" => ".pdf",
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".bin"
        };
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var dateFolder = DateTime.UtcNow.ToString("yyyy/MM");
        var relativeKey = Path.Combine(dateFolder, storedName).Replace(Path.DirectorySeparatorChar, '/');
        var absolutePath = ResolvePath(relativeKey);

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        try
        {
            long written;
            await using (var output = new FileStream(
                absolutePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                await content.CopyToAsync(output, cancellationToken);
                await output.FlushAsync(cancellationToken);
                written = output.Length;
            }

            return new StoredFileWriteResult(
                "Local",
                relativeKey,
                storedName,
                written);
        }
        catch
        {
            // Dispose the output stream before cleanup; a failed copy must not
            // leave an untracked partial file containing sensitive patient data.
            try
            {
                if (File.Exists(absolutePath))
                    File.Delete(absolutePath);
            }
            catch (IOException)
            {
                // Preserve the original write exception; persistent/orphaned blobs
                // still require durable reconciliation (release issue #120).
            }
            catch (UnauthorizedAccessException)
            {
                // Cleanup failures are separately addressed by the outbox/reconciler.
            }

            throw;
        }
    }

    public Task<Stream> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var absolutePath = ResolvePath(storageKey);
        Stream stream = new FileStream(
            absolutePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult(stream);
    }

    public Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var absolutePath = ResolvePath(storageKey);
        if (File.Exists(absolutePath))
            File.Delete(absolutePath);

        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        // Storage keys are server-generated relative paths, never arbitrary filesystem
        // paths. Reject both separator forms on every OS, including traversal keys.
        if (string.IsNullOrWhiteSpace(storageKey) ||
            Path.IsPathRooted(storageKey) ||
            storageKey.Split(['/', '\\']).Any(part =>
                part.Length == 0 || part is "." or ".." || part.Contains(':')))
            throw new InvalidOperationException("Invalid file storage key.");

        var normalizedKey = storageKey.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalizedKey));

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!fullPath.StartsWith(_rootPath + Path.DirectorySeparatorChar, comparison))
            throw new InvalidOperationException("Invalid file storage key.");

        // Lexical containment is not enough when an existing child directory or
        // file is a symlink/junction to a location outside of our storage root.
        var current = _rootPath;
        foreach (var part in normalizedKey.Split(Path.DirectorySeparatorChar))
        {
            current = Path.Combine(current, part);
            if (File.Exists(current) || Directory.Exists(current))
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Linked file storage paths are not allowed.");
            }
        }

        return fullPath;
    }
}
