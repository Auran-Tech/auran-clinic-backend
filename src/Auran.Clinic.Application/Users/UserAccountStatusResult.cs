namespace Auran.Clinic.Application.Users;

public sealed record UserAccountStatusResult(bool Success, string? ErrorCode)
{
    public static UserAccountStatusResult Succeeded() => new(true, null);
    public static UserAccountStatusResult Failed(string errorCode) => new(false, errorCode);
}
