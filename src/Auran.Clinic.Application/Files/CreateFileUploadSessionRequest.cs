namespace Auran.Clinic.Application.Files;

public sealed class CreateFileUploadSessionRequest
{
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long Size { get; set; }
}
