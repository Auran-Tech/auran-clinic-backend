using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Auran.Clinic.Infrastructure.Authentication;

public sealed class AuthService(
    AuranClinicDbContext dbContext,
    UserManager<ApplicationIdentityUser> userManager,
    IOptions<JwtOptions> jwtOptions,
    IAuditService auditService) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var identityUser = await userManager.FindByEmailAsync(request.Email.Trim());
        if (identityUser is null)
            return null;

        var user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdentityUserId == identityUser.Id, cancellationToken);
        if (user is null)
            return null;

        if (!await userManager.CheckPasswordAsync(identityUser, request.Password))
        {
            await auditService.WriteAsync(new AuditEvent
            {
                ClinicId = user.ClinicId,
                ActorUserId = user.Id,
                Action = "Authentication.LoginFailed",
                Category = "Security",
                EntityType = nameof(User),
                EntityId = user.Id.ToString(),
                Description = "Login failed because the supplied credentials were invalid."
            }, cancellationToken);
            return null;
        }

        var clinicIsActive = await dbContext.Clinics.AsNoTracking()
            .Where(x => x.Id == user.ClinicId)
            .Select(x => x.IsActive)
            .SingleOrDefaultAsync(cancellationToken);
        if (!clinicIsActive)
        {
            await auditService.WriteAsync(new AuditEvent
            {
                ClinicId = user.ClinicId,
                ActorUserId = user.Id,
                Action = "Authentication.LoginBlocked",
                Category = "Security",
                EntityType = nameof(User),
                EntityId = user.Id.ToString(),
                Description = "Login was blocked because the clinic is inactive."
            }, cancellationToken);
            return null;
        }

        var response = await CreateSessionAsync(user, identityUser, cancellationToken);
        await auditService.WriteAsync(new AuditEvent
        {
            ClinicId = user.ClinicId,
            ActorUserId = user.Id,
            Action = "Authentication.LoginSucceeded",
            Category = "Security",
            EntityType = nameof(User),
            EntityId = user.Id.ToString(),
            Description = "User signed in successfully."
        }, cancellationToken);

        return response;
    }

    public async Task<AuthResponse?> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var refreshToken = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null)
            return null;

        if (!refreshToken.IsActive)
        {
            await auditService.WriteAsync(new AuditEvent
            {
                ClinicId = refreshToken.ClinicId,
                ActorUserId = refreshToken.UserId,
                Action = "Authentication.RefreshFailed",
                Category = "Security",
                EntityType = nameof(RefreshToken),
                EntityId = refreshToken.Id.ToString(),
                Description = "Refresh token reuse or expiry was rejected."
            }, cancellationToken);
            return null;
        }

        var clinicIsActive = await dbContext.Clinics.AsNoTracking()
            .Where(x => x.Id == refreshToken.ClinicId)
            .Select(x => x.IsActive)
            .SingleOrDefaultAsync(cancellationToken);
        if (!clinicIsActive)
            return null;

        var user = await dbContext.Users.SingleOrDefaultAsync(
            x => x.Id == refreshToken.UserId && x.ClinicId == refreshToken.ClinicId,
            cancellationToken);
        if (user is null)
            return null;

        var identityUser = await userManager.FindByIdAsync(user.IdentityUserId);
        if (identityUser is null)
            return null;

        refreshToken.RevokedDate = DateTime.UtcNow;
        var response = await CreateSessionAsync(user, identityUser, cancellationToken, saveChanges: false);
        refreshToken.ReplacedByTokenHash = HashToken(response.RefreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(new AuditEvent
        {
            ClinicId = user.ClinicId,
            ActorUserId = user.Id,
            Action = "Authentication.TokenRefreshed",
            Category = "Security",
            EntityType = nameof(User),
            EntityId = user.Id.ToString(),
            Description = "Access and refresh tokens were rotated successfully."
        }, cancellationToken);

        return response;
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var entity = await dbContext.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (entity is null || entity.RevokedDate is not null)
            return;

        entity.RevokedDate = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(new AuditEvent
        {
            ClinicId = entity.ClinicId,
            ActorUserId = entity.UserId,
            Action = "Authentication.Logout",
            Category = "Security",
            EntityType = nameof(User),
            EntityId = entity.UserId.ToString(),
            Description = "User refresh token was revoked during logout."
        }, cancellationToken);
    }

    private async Task<AuthResponse> CreateSessionAsync(
        User user,
        ApplicationIdentityUser identityUser,
        CancellationToken cancellationToken,
        bool saveChanges = true)
    {
        var roleIds = await dbContext.UserRoles.AsNoTracking()
            .Where(x => x.ClinicId == user.ClinicId && x.UserId == user.Id)
            .Select(x => x.RoleId)
            .ToListAsync(cancellationToken);

        var roles = await dbContext.Roles.AsNoTracking()
            .Where(x => x.ClinicId == user.ClinicId && roleIds.Contains(x.Id))
            .Select(x => x.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        var permissions = user.IsSuperUser
            ? new List<string>()
            : await (from rolePermission in dbContext.RolePermissions.AsNoTracking()
                     join permission in dbContext.Permissions.AsNoTracking() on rolePermission.PermissionId equals permission.Id
                     where rolePermission.ClinicId == user.ClinicId && roleIds.Contains(rolePermission.RoleId)
                     select permission.Code)
                .Distinct()
                .ToListAsync(cancellationToken);

        var expiresDate = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);
        var accessToken = CreateAccessToken(user, identityUser, roles, permissions, expiresDate);
        var rawRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            ClinicId = user.ClinicId,
            UserId = user.Id,
            TokenHash = HashToken(rawRefreshToken),
            ExpiresDate = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays)
        });

        if (saveChanges)
            await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            AccessTokenExpiresDate = expiresDate,
            User = new CurrentUserResponse
            {
                UserId = user.Id,
                ClinicId = user.ClinicId,
                FullName = user.FullName,
                Email = user.Email,
                IsSuperUser = user.IsSuperUser,
                Roles = roles,
                Permissions = permissions
            }
        };
    }

    private string CreateAccessToken(
        User user,
        ApplicationIdentityUser identityUser,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        DateTime expiresDate)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, identityUser.Id),
            new("user_id", user.Id.ToString()),
            new("clinic_id", user.ClinicId.ToString()),
            new("super_user", user.IsSuperUser.ToString().ToLowerInvariant()),
            new(JwtRegisteredClaimNames.Email, identityUser.Email ?? user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims, expires: expiresDate, signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
