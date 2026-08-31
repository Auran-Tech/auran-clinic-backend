using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Authorization;
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
    IEffectivePermissionService effectivePermissionService,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var identityUser = await userManager.FindByEmailAsync(request.Email.Trim());
        if (identityUser is null || !await userManager.CheckPasswordAsync(identityUser, request.Password))
            return null;

        var user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdentityUserId == identityUser.Id, cancellationToken);
        if (user is null)
            return null;

        return await CreateSessionAsync(user, identityUser, cancellationToken);
    }

    public async Task<AuthResponse?> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var refreshToken = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive)
            return null;

        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == refreshToken.UserId && x.ClinicId == refreshToken.ClinicId, cancellationToken);
        if (user is null)
            return null;

        var identityUser = await userManager.FindByIdAsync(user.IdentityUserId);
        if (identityUser is null)
            return null;

        refreshToken.RevokedDate = DateTime.UtcNow;
        var response = await CreateSessionAsync(user, identityUser, cancellationToken, saveChanges: false);
        refreshToken.ReplacedByTokenHash = HashToken(response.RefreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);
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
    }

    private async Task<AuthResponse> CreateSessionAsync(User user, ApplicationIdentityUser identityUser, CancellationToken cancellationToken, bool saveChanges = true)
    {
        var roleIds = await dbContext.UserRoles.AsNoTracking()
            .Where(x => x.ClinicId == user.ClinicId && x.UserId == user.Id)
            .Select(x => x.RoleId)
            .ToListAsync(cancellationToken);

        var roles = await dbContext.Roles.AsNoTracking()
            .Where(x => roleIds.Contains(x.Id))
            .Select(x => x.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        var permissions = await effectivePermissionService.GetAsync(
            user.IsSuperUser,
            roleIds,
            cancellationToken);

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

    private string CreateAccessToken(User user, ApplicationIdentityUser identityUser, IEnumerable<string> roles, IEnumerable<string> permissions, DateTime expiresDate)
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
