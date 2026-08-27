namespace Auran.Clinic.Domain.Entities;

public class RolePermission : ClinicEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}
