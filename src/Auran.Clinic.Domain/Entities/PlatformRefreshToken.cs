using Auran.Clinic.Domain.Common;

namespace Auran.Clinic.Domain.Entities;

public class PlatformRefreshToken : BaseEntity
{
    public Guid PlatformUserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTime ExpiresDate { get; set; }
    public DateTime? RevokedDate { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive => RevokedDate is null && ExpiresDate > DateTime.UtcNow;
}
