namespace Auran.Clinic.Application.Clinics;

public sealed class CreateClinicRequest
{
    public required string Name { get; set; }
    public required string CodePrefix { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? FontFamily { get; set; }
    public string? WelcomeTitle { get; set; }
    public string? WelcomeMessage { get; set; }
    public string? WelcomeButtonText { get; set; }
    public string? TimeZoneId { get; set; }
    public string? CountryCode { get; set; }
    public string? CityCode { get; set; }
    public required string PatientNumberPrefix { get; set; }
    public string? Locale { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }
    public required InitialAdminRequest Admin { get; set; }
}
