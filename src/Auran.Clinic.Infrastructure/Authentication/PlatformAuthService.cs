using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Auran.Clinic.Infrastructure.Authentication;

public sealed class PlatformAuthService(
    AuranClinicDbContext dbContext,
    UserManager<ApplicationIdentityUser> userManager,
    SignInManager<ApplicationIdentityUser> signInManager,
    IHttpContextAccessor httpContextAccessor,
    IOptions<JwtOptions> jwtOptions) : IPlatformAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<PlatformAuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var identityUser = await userManager.FindByEmailAsync(request.Email.Trim());
        if (identityUser is null || identityUser.AccountType != AccountType.Platform)
            return null;

        var signInResult = await signInManager.CheckPasswordSignInAsync(identityUser, request.Password, lockoutOnFailure: true);
        if (!signInResult.Succeeded)
            return null;

        var platformUser = await dbContext.PlatformUsers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdentityUserId == identityUser.Id && x.IsActive, cancellationToken);
        if (platformUser is null)
            return null;

        return await CreateSessionAsync(platformUser, identityUser, cancellationToken);
    }

    public async Task<PlatformAuthResponse?> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return null;

        var tokenHash = HashToken(request.RefreshToken);
        var now = DateTime.UtcNow;
        var token = await dbContext.PlatformRefreshTokens.AsNoTracking()
            .Where(x => x.TokenHash == tokenHash && x.RevokedDate == null && x.ExpiresDate > now)
            .Select(x => new { x.Id, x.PlatformUserId })
            .SingleOrDefaultAsync(cancellationToken);
        if (token is null)
            return null;

        var platformUser = await dbContext.PlatformUsers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == token.PlatformUserId && x.IsActive, cancellationToken);
        if (platformUser is null)
            return null;

        var identityUser = await userManager.FindByIdAsync(platformUser.IdentityUserId);
        if (identityUser is null || identityUser.AccountType != AccountType.Platform)
            return null;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var revokedRows = await dbContext.PlatformRefreshTokens
            .Where(x => x.Id == token.Id && x.RevokedDate == null && x.ExpiresDate > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RevokedDate, now), cancellationToken);
        if (revokedRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var response = await CreateSessionAsync(platformUser, identityUser, cancellationToken, saveChanges: false);
        var replacementHash = HashToken(response.RefreshToken);
        await dbContext.PlatformRefreshTokens
            .Where(x => x.Id == token.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ReplacedByTokenHash, replacementHash), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return response;
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentPlatformUserId(out var platformUserId) || string.IsNullOrWhiteSpace(refreshToken))
            return;

        var tokenHash = HashToken(refreshToken);
        var now = DateTime.UtcNow;
        await dbContext.PlatformRefreshTokens
            .Where(x => x.PlatformUserId == platformUserId && x.TokenHash == tokenHash && x.RevokedDate == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RevokedDate, now), cancellationToken);
    }

    public async Task<PlatformCurrentUserResponse?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentPlatformUserId(out var platformUserId))
            return null;

        return await dbContext.PlatformUsers.AsNoTracking()
            .Where(x => x.Id == platformUserId && x.IsActive)
            .Select(x => new PlatformCurrentUserResponse
            {
                PlatformUserId = x.Id,
                FullName = x.FullName,
                Email = x.Email
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<PlatformAuthResponse> CreateSessionAsync(
        PlatformUser platformUser,
        ApplicationIdentityUser identityUser,
        CancellationToken cancellationToken,
        bool saveChanges = true)
    {
        var sessionId = Guid.NewGuid();
        var expiresDate = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var accessToken = CreateAccessToken(platformUser, identityUser, sessionId, expiresDate);

        dbContext.PlatformRefreshTokens.Add(new PlatformRefreshToken
        {
            Id = sessionId,
            PlatformUserId = platformUser.Id,
            TokenHash = HashToken(refreshToken),
            ExpiresDate = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays)
        });

        if (saveChanges)
            await dbContext.SaveChangesAsync(cancellationToken);

        return new PlatformAuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresDate = expiresDate,
            User = new PlatformCurrentUserResponse
            {
                PlatformUserId = platformUser.Id,
                FullName = platformUser.FullName,
                Email = platformUser.Email
            }
        };
    }

    private string CreateAccessToken(
        PlatformUser platformUser,
        ApplicationIdentityUser identityUser,
        Guid sessionId,
        DateTime expiresDate)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, identityUser.Id),
            new Claim(ActorPolicies.ActorTypeClaim, ActorPolicies.PlatformActor),
            new Claim("platform_user_id", platformUser.Id.ToString()),
            new Claim("session_id", sessionId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, identityUser.Email ?? platformUser.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims, expires: expiresDate, signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private bool TryGetCurrentPlatformUserId(out Guid platformUserId)
    {
        platformUserId = Guid.Empty;
        var principal = httpContextAccessor.HttpContext?.User;
        return principal?.Identity?.IsAuthenticated == true &&
               principal.FindFirstValue(ActorPolicies.ActorTypeClaim) == ActorPolicies.PlatformActor &&
               Guid.TryParse(principal.FindFirstValue("platform_user_id"), out platformUserId);
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));
}
