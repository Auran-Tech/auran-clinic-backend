namespace Auran.Clinic.Infrastructure.Files;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string Provider { get; init; } = "Local";
    public string LocalRootPath { get; init; } = "App_Data/uploads";
    public long MaxFileSizeBytes { get; init; } = 20 * 1024 * 1024;
}
