using Auran.Clinic.Application.Files;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.Infrastructure.Files;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string RootPath { get; init; } = "App_Data/uploads";
    public bool OrphanCleanupEnabled { get; init; } = false;
    public int OrphanGracePeriodHours { get; init; } = 24;
    public int OrphanCleanupIntervalMinutes { get; init; } = 60;
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

    /// <summary>
    /// Enumerates only old, locally generated storage keys. A final SQL record
    /// existence check must be made by the caller immediately before deletion.
    /// </summary>
    public IReadOnlyList<string> GetOrphanCandidates(DateTime olderThanUtc)
    {
        if (!Directory.Exists(_rootPath))
            return [];

        var results = new List<string>();
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = false,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        foreach (var path in Directory.EnumerateFiles(_rootPath, "*", options))
        {
            var relative = Path.GetRelativePath(_rootPath, path)
                .Replace(Path.DirectorySeparatorChar, '/');
            var parts = relative.Split('/');
            if (parts.Length != 3 ||
                parts[0].Length != 4 || !int.TryParse(parts[0], out var year) ||
                year < 2000 || year > 9999 ||
                parts[1].Length != 2 || !int.TryParse(parts[1], out var month) ||
                month is < 1 or > 12)
                continue;

            var extension = Path.GetExtension(parts[2]);
            if (extension is not (".pdf" or ".jpg" or ".png" or ".webp" or ".bin"))
                continue;

            var filename = Path.GetFileNameWithoutExtension(parts[2]);
            if (filename.Length != 32 || !Guid.TryParseExact(filename, "N", out _))
                continue;

            // Revalidate every existing component against symlink escapes.
            try
            {
                _ = ResolvePath(relative);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            if (File.GetLastWriteTimeUtc(path) < olderThanUtc)
                results.Add(relative);
        }

        return results;
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
