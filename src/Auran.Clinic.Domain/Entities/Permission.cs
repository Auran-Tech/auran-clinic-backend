using Auran.Clinic.Domain.Common;

namespace Auran.Clinic.Domain.Entities;

public class Permission : BaseEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Group { get; set; }
}
