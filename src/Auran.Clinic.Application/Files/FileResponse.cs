using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Application.Files;

public sealed class FileResponse
{
    public Guid Id { get; set; }
    public required string FileName { get; set; }
    public required string FileExtension { get; set; }
    public required string ContentType { get; set; }
    public long Size { get; set; }
    public FileStorageProvider StorageProvider { get; set; }
    public required string Url { get; set; }
    public DateTime UploadedAtUtc { get; set; }
}

public sealed class FileDownloadResponse
{
    public required Stream Content { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
}

public sealed class FileUploadContentResult
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
}
