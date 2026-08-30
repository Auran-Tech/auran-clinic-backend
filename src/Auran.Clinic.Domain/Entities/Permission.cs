using Auran.Clinic.Domain.Common;

namespace Auran.Clinic.Domain.Entities;

public class Permission : BaseEntity
{
    public required string Key { get; set; }

    public required string GroupKey { get; set; }
}
