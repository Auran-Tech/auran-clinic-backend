namespace Auran.Clinic.Application.Authentication;

public sealed class AuthResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public DateTime AccessTokenExpiresDate { get; init; }
    public required CurrentUserResponse User { get; init; }
}
