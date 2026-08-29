using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Domain.Entities;

public class FileRecord : ClinicEntity
{
    public required string OriginalName { get; set; }
    public required string StoredName { get; set; }
    public required string FileExtension { get; set; }
    public required string ContentType { get; set; }
    public long Size { get; set; }
    public FileStorageProvider StorageProvider { get; set; }
    public required string StorageKey { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public Guid UploadedByUserId { get; set; }
}
