namespace Auran.Clinic.Application.Users;

public enum UserAccountStatusOutcome
{
    Success,
    NotFound,
    Forbidden,
    Unauthenticated
}

public sealed record UserAccountStatusResult(
    UserAccountStatusOutcome Outcome,
    UserAccountStatusResponse? User = null);

public sealed record UserAccountStatusResponse(Guid UserId, bool IsActive);
