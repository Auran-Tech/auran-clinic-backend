namespace Auran.Clinic.Domain.Entities;

public class UserRole : ClinicEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}
