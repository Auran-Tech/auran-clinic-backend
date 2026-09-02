namespace Auran.Clinic.Application.Users;

public enum UserManagementOutcome
{
    Success,
    NotFound,
    Forbidden,
    Conflict,
    ValidationError,
    Unauthenticated
}

public sealed record UserManagementResult(
    UserManagementOutcome Outcome,
    UserAccountResponse? User = null,
    string? Error = null);
