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
