namespace Auran.Clinic.Application.Authentication;

public interface IPlatformAuthService
{
    Task<PlatformAuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<PlatformAuthResponse?> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid platformUserId, string refreshToken, CancellationToken cancellationToken = default);
}
