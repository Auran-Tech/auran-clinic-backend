using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Auran.Clinic.Application.Auditing;
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
    IOptions<JwtOptions> jwtOptions,
    IAuditService auditService) : IPlatformAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<PlatformAuthResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var identityUser = await userManager.FindByEmailAsync(request.Email.Trim());
        if (identityUser is null || identityUser.AccountType != AccountType.Platform)
            return null;

        var platformUser = await dbContext.PlatformUsers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdentityUserId == identityUser.Id, cancellationToken);
        if (platformUser is null || !platformUser.IsActive)
            return null;

        if (!await userManager.CheckPasswordAsync(identityUser, request.Password))
        {
            await WriteAuditAsync(platformUser, identityUser, "PlatformAuthentication.LoginFailed",
                "Platform login failed because the supplied credentials were invalid.", cancellationToken);
            return null;
        }

        var response = await CreateSessionAsync(platformUser, identityUser, cancellationToken);
        await WriteAuditAsync(platformUser, identityUser, "PlatformAuthentication.LoginSucceeded",
            "Platform user signed in successfully.", cancellationToken);

        return response;
    }

    public async Task<PlatformAuthResponse?> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return null;

        var rawRefreshToken = request.RefreshToken.Trim();
        var tokenHash = HashToken(rawRefreshToken);
        var now = DateTime.UtcNow;

        var refreshToken = await dbContext.PlatformRefreshTokens.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (refreshToken is null || refreshToken.RevokedDate is not null || refreshToken.ExpiresDate <= now)
            return null;

        var platformUser = await dbContext.PlatformUsers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == refreshToken.PlatformUserId && x.IsActive, cancellationToken);
        if (platformUser is null)
            return null;

        var identityUser = await userManager.FindByIdAsync(platformUser.IdentityUserId);
        if (identityUser is null || identityUser.AccountType != AccountType.Platform)
            return null;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var revokedRows = await dbContext.PlatformRefreshTokens
                .Where(x => x.Id == refreshToken.Id && x.RevokedDate == null && x.ExpiresDate > now)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.RevokedDate, now),
                    cancellationToken);

            if (revokedRows != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var response = await CreateSessionAsync(
                platformUser,
                identityUser,
                cancellationToken,
                saveChanges: false);

            await dbContext.SaveChangesAsync(cancellationToken);

            var replacementHash = HashToken(response.RefreshToken);
            await dbContext.PlatformRefreshTokens
                .Where(x => x.Id == refreshToken.Id)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.ReplacedByTokenHash, replacementHash),
                    cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            await WriteAuditAsync(platformUser, identityUser, "PlatformAuthentication.TokenRefreshed",
                "Platform access and refresh tokens were rotated successfully.", cancellationToken);

            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var tokenHash = HashToken(refreshToken.Trim());
        var entity = await dbContext.PlatformRefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (entity is null || entity.RevokedDate is not null)
            return;

        entity.RevokedDate = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var platformUser = await dbContext.PlatformUsers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == entity.PlatformUserId, cancellationToken);
        if (platformUser is null)
            return;

        var identityUser = await userManager.FindByIdAsync(platformUser.IdentityUserId);
        if (identityUser is null)
            return;

        await WriteAuditAsync(platformUser, identityUser, "PlatformAuthentication.Logout",
            "Platform refresh token was revoked during logout.", cancellationToken);
    }

    private async Task<PlatformAuthResponse> CreateSessionAsync(
        PlatformUser platformUser,
        ApplicationIdentityUser identityUser,
        CancellationToken cancellationToken,
        bool saveChanges = true)
    {
        var roleIds = await dbContext.PlatformUserRoles.AsNoTracking()
            .Where(x => x.PlatformUserId == platformUser.Id)
            .Select(x => x.PlatformRoleId)
            .ToListAsync(cancellationToken);

        var roles = await dbContext.PlatformRoles.AsNoTracking()
            .Where(x => roleIds.Contains(x.Id))
            .Select(x => x.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        var permissions = await (from rolePermission in dbContext.PlatformRolePermissions.AsNoTracking()
                                 join permission in dbContext.Permissions.AsNoTracking()
                                     on rolePermission.PermissionId equals permission.Id
                                 where roleIds.Contains(rolePermission.PlatformRoleId)
                                       && permission.Scope == PermissionScope.Platform
                                 select permission.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var expiresDate = now.AddMinutes(_jwt.AccessTokenMinutes);
        var accessToken = CreateAccessToken(platformUser, identityUser, roles, permissions, expiresDate);
        var rawRefreshToken = CreateRefreshToken();

        dbContext.PlatformRefreshTokens.Add(new PlatformRefreshToken
        {
            Id = Guid.NewGuid(),
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
                Email = platformUser.Email,
                Roles = roles,
                Permissions = permissions
            }
        };
    }

    private string CreateAccessToken(
        PlatformUser platformUser,
        ApplicationIdentityUser identityUser,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        DateTime expiresDate)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, identityUser.Id),
            new(ClaimTypes.NameIdentifier, identityUser.Id),
            new("actor_type", ActorType.Platform.ToString()),
            new("platform_user_id", platformUser.Id.ToString()),
            new("display_name", platformUser.FullName),
            new(ClaimTypes.Email, identityUser.Email ?? platformUser.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim("platform_role", role)));
        claims.AddRange(permissions.Select(permission => new Claim("platform_permission", permission)));

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

    private Task WriteAuditAsync(
        PlatformUser platformUser,
        ApplicationIdentityUser identityUser,
        string action,
        string description,
        CancellationToken cancellationToken) =>
        auditService.WriteAsync(new AuditEvent
        {
            Scope = AuditScope.Platform,
            ActorType = ActorType.Platform,
            ActorId = platformUser.Id,
            ActorIdentityUserId = identityUser.Id,
            ActorDisplayName = platformUser.FullName,
            ActorEmail = identityUser.Email ?? platformUser.Email,
            Action = action,
            Category = "Security",
            EntityType = nameof(PlatformUser),
            EntityId = platformUser.Id.ToString(),
            Description = description
        }, cancellationToken);

    private static string CreateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));
}
