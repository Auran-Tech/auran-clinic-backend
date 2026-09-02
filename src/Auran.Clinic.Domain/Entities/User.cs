using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Domain.Entities;

public class User : ClinicEntity
{
    public required string IdentityUserId { get; set; }
    public AccountType IdentityAccountType { get; private set; } = AccountType.Clinic;
    public required string FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsSuperUser { get; set; }
    public bool IsActive { get; set; } = true;
}
