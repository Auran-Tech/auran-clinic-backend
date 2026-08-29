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
    public required string TimeZoneId { get; set; }
    public required string CountryCode { get; set; }
    public required string CityCode { get; set; }
    public required string PatientNumberPrefix { get; set; }
    public required string Locale { get; set; }
    public required string Phone { get; set; }
    public required string Email { get; set; }
    public required string Address { get; set; }
    public string? Website { get; set; }
    public required InitialAdminRequest Admin { get; set; }
}
