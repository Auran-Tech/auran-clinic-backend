namespace Auran.Clinic.Application.Users;

public sealed record UserAccountResponse(
    Guid Id,
    string FullName,
    string? Email,
    string? Phone,
    bool IsActive,
    bool IsSuperUser,
    IReadOnlyList<string> Roles);
