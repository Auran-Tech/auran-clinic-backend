namespace Auran.Clinic.Domain.Entities;

public class Patient : ClinicEntity
{
    public required string PatientNumber { get; set; }
    public required string FullName { get; set; }
    public required string Phone { get; set; }
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Notes { get; set; }
}

public class PatientCondition : ClinicEntity
{
    public Guid PatientId { get; set; }
    public required string Name { get; set; }
    public string? Notes { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public Guid RecordedByUserId { get; set; }
}

public class PatientAllergy : ClinicEntity
{
    public Guid PatientId { get; set; }
    public required string Name { get; set; }
    public string? Reaction { get; set; }
    public string? Notes { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public Guid RecordedByUserId { get; set; }
}

public class PatientMedication : ClinicEntity
{
    public Guid PatientId { get; set; }
    public required string Name { get; set; }
    public string? Dosage { get; set; }
    public string? Notes { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public Guid RecordedByUserId { get; set; }
}
