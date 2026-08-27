using Auran.Clinic.Domain.Common;

namespace Auran.Clinic.Domain.Entities;

public class PlatformRole : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsSystem { get; set; } = true;
}
