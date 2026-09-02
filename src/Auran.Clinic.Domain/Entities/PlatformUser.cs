using Auran.Clinic.Domain.Common;
using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Domain.Entities;

public class PlatformUser : BaseEntity
{
    public required string IdentityUserId { get; set; }
    public AccountType IdentityAccountType { get; private set; } = AccountType.Platform;
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
}
