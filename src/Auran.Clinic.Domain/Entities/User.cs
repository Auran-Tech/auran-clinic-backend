namespace Auran.Clinic.Domain.Entities;

public class User : ClinicEntity
{
    public required string IdentityUserId { get; set; }
    public required string FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsClinicSuperUser { get; set; }
    public bool IsActive { get; set; } = true;
}
