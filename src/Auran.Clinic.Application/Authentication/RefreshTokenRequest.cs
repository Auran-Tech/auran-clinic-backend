namespace Auran.Clinic.Application.Authentication;

public sealed class RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}
