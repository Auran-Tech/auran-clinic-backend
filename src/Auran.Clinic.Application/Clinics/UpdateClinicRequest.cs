namespace Auran.Clinic.Application.Clinics;

public sealed class UpdateClinicRequest
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? FontFamily { get; set; }
    public string? WelcomeTitle { get; set; }
    public string? WelcomeMessage { get; set; }
    public string? TimeZoneId { get; set; }
    public required string PatientNumberPrefix { get; set; }
}
