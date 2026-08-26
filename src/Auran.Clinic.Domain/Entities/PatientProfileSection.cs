namespace Auran.Clinic.Domain.Entities;

public class PatientProfileSection : ClinicEntity
{
    public required string Name { get; set; }
    public int SortOrder { get; set; }
    public bool IsSystem { get; set; }
    public bool IsEnabled { get; set; } = true;
}
