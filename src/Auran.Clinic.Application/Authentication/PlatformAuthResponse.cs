namespace Auran.Clinic.Application.Authentication;

public sealed class PlatformAuthResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public DateTime AccessTokenExpiresDate { get; init; }
    public required PlatformCurrentUserResponse User { get; init; }
}
