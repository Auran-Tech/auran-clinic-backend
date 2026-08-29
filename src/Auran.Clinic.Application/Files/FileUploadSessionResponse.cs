using System.Text.Json.Serialization;
using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Application.Files;

public sealed class FileUploadSessionResponse
{
    public Guid SessionId { get; set; }
    public required string UploadUrl { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public FileStorageProvider StorageProvider { get; set; }
    public long ExpectedSize { get; set; }

    [JsonIgnore]
    public string? UploadToken { get; set; }
}
