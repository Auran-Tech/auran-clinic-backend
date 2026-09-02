namespace Auran.Clinic.Application.Authentication;

public sealed class PlatformAuthResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public DateTime AccessTokenExpiresDate { get; init; }
    public required PlatformCurrentUserResponse User { get; init; }
}

public sealed class PlatformCurrentUserResponse
{
    public Guid PlatformUserId { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
}
