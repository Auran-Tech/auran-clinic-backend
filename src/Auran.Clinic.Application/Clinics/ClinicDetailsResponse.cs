namespace Auran.Clinic.Application.Clinics;

public sealed class ClinicDetailsResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public bool IsActive { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? FontFamily { get; set; }
    public string? WelcomeTitle { get; set; }
    public string? WelcomeMessage { get; set; }
    public string? TimeZoneId { get; set; }
    public string? PatientNumberPrefix { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public ClinicSettingsResponse? Settings { get; set; }
    public InitialAdminResponse? InitialAdmin { get; set; }
}
