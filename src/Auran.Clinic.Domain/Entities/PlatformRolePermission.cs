using Auran.Clinic.Domain.Common;

namespace Auran.Clinic.Domain.Entities;

public class PlatformRolePermission : BaseEntity
{
    public Guid PlatformRoleId { get; set; }
    public Guid PermissionId { get; set; }
}
