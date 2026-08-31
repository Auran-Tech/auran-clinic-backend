using Auran.Clinic.Domain.Common;
using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Domain.Entities;

public class Permission : BaseEntity
{
    public required string Key { get; set; }
    public required string GroupKey { get; set; }
    public PermissionScope Scope { get; set; }
}
