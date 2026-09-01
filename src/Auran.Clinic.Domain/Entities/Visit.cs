using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Domain.Entities;

public class Visit : ClinicEntity
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public VisitStatus Status { get; set; } = VisitStatus.Open;
    public DocumentationStatus DocumentationStatus { get; set; } = DocumentationStatus.NotStarted;
    public DateTime EntryAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? ExitAtUtc { get; set; }
    public string? ChiefComplaint { get; set; }
    public string? Examination { get; set; }
    public string? Diagnosis { get; set; }
    public string? Notes { get; set; }
    public string? TreatmentPlan { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
