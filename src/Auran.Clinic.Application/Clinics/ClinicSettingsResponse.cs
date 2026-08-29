namespace Auran.Clinic.Application.Clinics;

public sealed class ClinicSettingsResponse
{
    public Guid Id { get; set; }
    public Guid ClinicId { get; set; }
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
    public string? PatientNumberPrefix { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }
    public string? Locale { get; set; }
    public string? DateFormat { get; set; }
    public string? TimeFormat { get; set; }
    public int DocumentationReminderHours { get; set; }
    public string? PrescriptionHeader { get; set; }
    public string? PrescriptionFooter { get; set; }
}
