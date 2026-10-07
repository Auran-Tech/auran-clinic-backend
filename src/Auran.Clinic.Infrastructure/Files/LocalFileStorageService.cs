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
        var extension = Path.GetExtension(Path.GetFileName(originalName));
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var dateFolder = DateTime.UtcNow.ToString("yyyy/MM");
        var relativeKey = Path.Combine(dateFolder, storedName).Replace(Path.DirectorySeparatorChar, '/');
        var absolutePath = ResolvePath(relativeKey);

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using var output = new FileStream(
            absolutePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await content.CopyToAsync(output, cancellationToken);
        await output.FlushAsync(cancellationToken);

        return new StoredFileWriteResult(
            "Local",
            relativeKey,
            storedName,
            output.Length);
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
        var normalizedKey = storageKey.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalizedKey));

        if (!fullPath.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid file storage key.");

        return fullPath;
    }
}
