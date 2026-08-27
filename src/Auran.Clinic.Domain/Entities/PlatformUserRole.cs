using Auran.Clinic.Domain.Common;

namespace Auran.Clinic.Domain.Entities;

public class PlatformUserRole : BaseEntity
{
    public Guid PlatformUserId { get; set; }
    public Guid PlatformRoleId { get; set; }
}
