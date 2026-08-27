namespace Auran.Clinic.Domain.Entities;

public class Role : ClinicEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsSystem { get; set; } = true;
}
