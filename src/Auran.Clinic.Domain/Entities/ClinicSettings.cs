using Auran.Clinic.Domain.Common;

namespace Auran.Clinic.Domain.Entities;

public class ClinicSettings : BaseEntity
{
    public Guid ClinicId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }
    public string? Locale { get; set; }
    public string? DateFormat { get; set; }
    public string? TimeFormat { get; set; }
    public int DocumentationReminderHours { get; set; } = 12;
    public string? PrescriptionHeader { get; set; }
    public string? PrescriptionFooter { get; set; }
    public string? WelcomeButtonText { get; set; }
}
