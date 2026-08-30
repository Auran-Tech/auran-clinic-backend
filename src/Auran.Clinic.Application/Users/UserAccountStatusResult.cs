namespace Auran.Clinic.Application.Users;

public sealed record UserAccountStatusResult(bool Success, string? ErrorCode = null)
{
    public static UserAccountStatusResult Succeeded() => new(true);

    public static UserAccountStatusResult Failed(string errorCode) => new(false, errorCode);
}
