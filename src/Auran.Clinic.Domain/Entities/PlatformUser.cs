using Auran.Clinic.Domain.Common;

namespace Auran.Clinic.Domain.Entities;

public class PlatformUser : BaseEntity
{
    public required string IdentityUserId { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
}
