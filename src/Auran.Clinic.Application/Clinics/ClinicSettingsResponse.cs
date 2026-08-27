namespace Auran.Clinic.Application.Clinics;

public sealed class ClinicSettingsResponse
{
    public Guid Id { get; set; }
    public Guid ClinicId { get; set; }
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
    public string? WelcomeButtonText { get; set; }
}
