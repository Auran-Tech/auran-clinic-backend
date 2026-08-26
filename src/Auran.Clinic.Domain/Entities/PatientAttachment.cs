namespace Auran.Clinic.Domain.Entities;

public class PatientAttachment : ClinicEntity
{
    public Guid PatientId { get; set; }
    public Guid FileId { get; set; }
    public string? Category { get; set; }
    public string? Notes { get; set; }
}
