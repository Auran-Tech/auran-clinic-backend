using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Infrastructure.Files;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public FileStorageProvider Provider { get; set; } = FileStorageProvider.Local;
    public string LocalRootPath { get; set; } = "storage";
    public int UploadSessionMinutes { get; set; } = 15;
    public int MaxFileSizeMb { get; set; } = 50;

    public long MaxFileSizeBytes => Math.Max(1, MaxFileSizeMb) * 1024L * 1024L;
}
