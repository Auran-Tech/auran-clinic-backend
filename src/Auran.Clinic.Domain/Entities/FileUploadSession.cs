using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Domain.Entities;

public class FileUploadSession : ClinicEntity
{
    public ActorType RequestedByActorType { get; set; }
    public Guid RequestedByActorId { get; set; }
    public required string OriginalName { get; set; }
    public required string FileExtension { get; set; }
    public required string ContentType { get; set; }
    public long ExpectedSize { get; set; }
    public FileStorageProvider StorageProvider { get; set; }
    public required string StorageKey { get; set; }
    public required string UploadTokenHash { get; set; }
    public FileUploadStatus Status { get; set; } = FileUploadStatus.Pending;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UploadedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public Guid? FileId { get; set; }
}
