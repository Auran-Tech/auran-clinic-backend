using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Auran.Clinic.Application.Abstractions;
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
    ICurrentUserContext currentUserContext,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var identityUser = await userManager.FindByEmailAsync(request.Email.Trim());
        if (identityUser is null || !await userManager.CheckPasswordAsync(identityUser, request.Password))
            return null;

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdentityUserId == identityUser.Id, cancellationToken);

        if (user is null || !user.IsActive || !await IsClinicActiveAsync(user.ClinicId, cancellationToken))
            return null;

        return await CreateSessionAsync(user, identityUser, cancellationToken);
    }

    public async Task<AuthResponse?> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var refreshToken = await dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive)
            return null;

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                x => x.Id == refreshToken.UserId &&
                     x.ClinicId == refreshToken.ClinicId &&
                     x.IsActive,
                cancellationToken);

        if (user is null || !await IsClinicActiveAsync(user.ClinicId, cancellationToken))
            return null;

        var identityUser = await userManager.FindByIdAsync(user.IdentityUserId);
        if (identityUser is null)
            return null;

        refreshToken.RevokedDate = DateTime.UtcNow;
        var response = await CreateSessionAsync(user, identityUser, cancellationToken, saveChanges: false);
        refreshToken.ReplacedByTokenHash = HashToken(response.RefreshToken);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return response;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return null;
        }
    }

    public async Task<CurrentUserResponse?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.IsAuthenticated ||
            currentUserContext.UserId is not Guid userId ||
            currentUserContext.ClinicId is not Guid clinicId)
        {
            return null;
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == userId && x.ClinicId == clinicId && x.IsActive, cancellationToken);

        if (user is null || !await IsClinicActiveAsync(clinicId, cancellationToken))
            return null;

        var (roles, permissions) = await GetAuthorizationContextAsync(user, cancellationToken);
        return MapCurrentUser(user, roles, permissions);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.IsAuthenticated ||
            currentUserContext.UserId is not Guid userId ||
            currentUserContext.ClinicId is not Guid clinicId)
        {
            return;
        }

        var tokenHash = HashToken(refreshToken);
        var entity = await dbContext.RefreshTokens.SingleOrDefaultAsync(
            x => x.TokenHash == tokenHash && x.UserId == userId && x.ClinicId == clinicId,
            cancellationToken);

        if (entity is null || entity.RevokedDate is not null)
            return;

        entity.RevokedDate = DateTime.UtcNow;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
        }
    }

    private async Task<AuthResponse> CreateSessionAsync(
        User user,
        ApplicationIdentityUser identityUser,
        CancellationToken cancellationToken,
        bool saveChanges = true)
    {
        var (roles, permissions) = await GetAuthorizationContextAsync(user, cancellationToken);
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
            User = MapCurrentUser(user, roles, permissions)
        };
    }

    private async Task<(List<string> Roles, List<string> Permissions)> GetAuthorizationContextAsync(
        User user,
        CancellationToken cancellationToken)
    {
        var roleIds = await dbContext.UserRoles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.ClinicId == user.ClinicId && x.UserId == user.Id)
            .Select(x => x.RoleId)
            .ToListAsync(cancellationToken);

        var roles = await dbContext.Roles.AsNoTracking()
            .Where(x => roleIds.Contains(x.Id))
            .Select(x => x.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        var permissions = user.IsSuperUser
            ? Permissions.All.Select(x => x.Key).Distinct(StringComparer.Ordinal).ToList()
            : await (from rolePermission in dbContext.RolePermissions.AsNoTracking()
                     join permission in dbContext.Permissions.AsNoTracking()
                         on rolePermission.PermissionId equals permission.Id
                     where roleIds.Contains(rolePermission.RoleId)
                     select permission.Key)
                .Distinct()
                .ToListAsync(cancellationToken);

        return (roles, permissions);
    }

    private async Task<bool> IsClinicActiveAsync(Guid clinicId, CancellationToken cancellationToken) =>
        await dbContext.Clinics.AsNoTracking()
            .AnyAsync(x => x.Id == clinicId && x.IsActive, cancellationToken);

    private static CurrentUserResponse MapCurrentUser(
        User user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions) =>
        new()
        {
            UserId = user.Id,
            ClinicId = user.ClinicId,
            FullName = user.FullName,
            Email = user.Email,
            IsSuperUser = user.IsSuperUser,
            Roles = roles,
            Permissions = permissions
        };

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
        var token = new JwtSecurityToken(
            _jwt.Issuer,
            _jwt.Audience,
            claims,
            expires: expiresDate,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
