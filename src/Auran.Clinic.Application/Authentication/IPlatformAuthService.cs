namespace Auran.Clinic.Application.Authentication;

public interface IPlatformAuthService
{
    Task<PlatformAuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<PlatformAuthResponse?> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
}
