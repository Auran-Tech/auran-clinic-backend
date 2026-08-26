using Auran.Clinic.Domain.Common;

namespace Auran.Clinic.Domain.Entities;

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}
