namespace Auran.Clinic.Domain.Entities;

public class PatientMedication : ClinicEntity
{
    public Guid PatientId { get; set; }
    public required string Name { get; set; }
    public string? Dosage { get; set; }
    public string? Notes { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public Guid RecordedByUserId { get; set; }
}
