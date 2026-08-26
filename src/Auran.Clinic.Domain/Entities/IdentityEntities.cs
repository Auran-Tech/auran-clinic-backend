using Auran.Clinic.Domain.Common;

namespace Auran.Clinic.Domain.Entities;

public class User : ClinicEntity
{
    public required string IdentityUserId { get; set; }
    public required string FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsSuperUser { get; set; }
}

public class Role : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsSystem { get; set; } = true;
}

public class Permission : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Group { get; set; }
}

public class UserRole : ClinicEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}
