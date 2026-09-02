namespace Auran.Clinic.Infrastructure.Platform;

public sealed class PlatformBootstrapOptions
{
    public const string SectionName = "PlatformBootstrap";

    public bool Enabled { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
