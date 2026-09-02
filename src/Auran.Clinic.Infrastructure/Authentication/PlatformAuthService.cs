using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Auran.Clinic.Infrastructure.Authentication;

public sealed class PlatformAuthService(
    AuranClinicDbContext dbContext,
    UserManager<ApplicationIdentityUser> userManager,
    SignInManager<ApplicationIdentityUser> signInManager,
    IOptions<JwtOptions> jwtOptions) : IPlatformAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<PlatformAuthResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var identityUser = await userManager.FindByEmailAsync(request.Email.Trim());
        if (identityUser is null || identityUser.AccountType != AccountType.Platform)
            return null;

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            identityUser,
            request.Password,
            lockoutOnFailure: true);
        if (!signInResult.Succeeded)
            return null;

        var platformUser = await dbContext.PlatformUsers.AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.IdentityUserId == identityUser.Id && user.IsActive,
                cancellationToken);
        if (platformUser is null)
            return null;

        return await CreateSessionAsync(platformUser, identityUser, cancellationToken);
    }

    public async Task<PlatformAuthResponse?> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return null;

        var tokenHash = HashToken(request.RefreshToken);
        var now = DateTime.UtcNow;
        var refreshToken = await dbContext.PlatformRefreshTokens.AsNoTracking()
            .Where(token =>
                token.TokenHash == tokenHash &&
                token.RevokedDate == null &&
                token.ExpiresDate > now)
            .Select(token => new { token.Id, token.PlatformUserId })
            .SingleOrDefaultAsync(cancellationToken);
        if (refreshToken is null)
            return null;

        var platformUser = await dbContext.PlatformUsers.AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.Id == refreshToken.PlatformUserId && user.IsActive,
                cancellationToken);
        if (platformUser is null)
            return null;

        var identityUser = await userManager.FindByIdAsync(platformUser.IdentityUserId);
        if (identityUser is null || identityUser.AccountType != AccountType.Platform)
            return null;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var revokedCount = await dbContext.PlatformRefreshTokens
            .Where(token =>
                token.Id == refreshToken.Id &&
                token.RevokedDate == null &&
                token.ExpiresDate > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedDate, now),
                cancellationToken);
        if (revokedCount != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var response = await CreateSessionAsync(
            platformUser,
            identityUser,
            cancellationToken,
            saveChanges: false);
        var replacementHash = HashToken(response.RefreshToken);

        await dbContext.PlatformRefreshTokens
            .Where(token => token.Id == refreshToken.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.ReplacedByTokenHash, replacementHash),
                cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return response;
    }

    public async Task RevokeAsync(
        Guid platformUserId,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var tokenHash = HashToken(refreshToken);
        var now = DateTime.UtcNow;
        await dbContext.PlatformRefreshTokens
            .Where(token =>
                token.TokenHash == tokenHash &&
                token.PlatformUserId == platformUserId &&
                token.RevokedDate == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedDate, now),
                cancellationToken);
    }

    private async Task<PlatformAuthResponse> CreateSessionAsync(
        PlatformUser platformUser,
        ApplicationIdentityUser identityUser,
        CancellationToken cancellationToken,
        bool saveChanges = true)
    {
        var now = DateTime.UtcNow;
        var sessionId = Guid.NewGuid();
        var expiresDate = now.AddMinutes(_jwt.AccessTokenMinutes);
        var rawRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var accessToken = CreateAccessToken(
            platformUser,
            identityUser,
            sessionId,
            expiresDate);

        dbContext.PlatformRefreshTokens.Add(new PlatformRefreshToken
        {
            Id = sessionId,
            PlatformUserId = platformUser.Id,
            TokenHash = HashToken(rawRefreshToken),
            ExpiresDate = now.AddDays(_jwt.RefreshTokenDays),
            CreatedDate = now
        });

        if (saveChanges)
            await dbContext.SaveChangesAsync(cancellationToken);

        return new PlatformAuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
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
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, identityUser.Id),
            new("actor_type", ActorType.Platform.ToString()),
            new("platform_user_id", platformUser.Id.ToString()),
            new("session_id", sessionId.ToString()),
            new(JwtRegisteredClaimNames.Email, identityUser.Email ?? platformUser.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _jwt.Issuer,
            _jwt.Audience,
            claims,
            expires: expiresDate,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));
}
