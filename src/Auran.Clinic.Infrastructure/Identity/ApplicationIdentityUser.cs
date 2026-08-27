using Auran.Clinic.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Auran.Clinic.Infrastructure.Identity;

public sealed class ApplicationIdentityUser : IdentityUser
{
    public AccountType AccountType { get; set; } = AccountType.Clinic;
}
