using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
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
    SignInManager<ApplicationIdentityUser> signInManager,
    IEffectivePermissionService effectivePermissionService,
    ICurrentUserContext currentUserContext,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var identityUser = await userManager.FindByEmailAsync(request.Email.Trim());
        if (identityUser is null || identityUser.AccountType != AccountType.Clinic)
            return null;

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            identityUser,
            request.Password,
            lockoutOnFailure: true);
        if (!signInResult.Succeeded)
            return null;

        var user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdentityUserId == identityUser.Id, cancellationToken);
        if (user is null || !await CanAuthenticateAsync(user, cancellationToken))
            return null;

        return await CreateSessionAsync(user, identityUser, cancellationToken);
    }

    public async Task<AuthResponse?> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var now = DateTime.UtcNow;
        var refreshToken = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(token => token.TokenHash == tokenHash && token.RevokedDate == null && token.ExpiresDate > now)
            .Select(token => new { token.Id, token.UserId, token.ClinicId })
            .SingleOrDefaultAsync(cancellationToken);
        if (refreshToken is null)
            return null;

        var user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == refreshToken.UserId && x.ClinicId == refreshToken.ClinicId, cancellationToken);
        if (user is null || !await CanAuthenticateAsync(user, cancellationToken))
            return null;

        var identityUser = await userManager.FindByIdAsync(user.IdentityUserId);
        if (identityUser is null || identityUser.AccountType != AccountType.Clinic)
            return null;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var revokedCount = await dbContext.RefreshTokens
            .Where(token => token.Id == refreshToken.Id && token.RevokedDate == null && token.ExpiresDate > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedDate, now), cancellationToken);
        if (revokedCount != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var response = await CreateSessionAsync(user, identityUser, cancellationToken, saveChanges: false);
        var replacementTokenHash = HashToken(response.RefreshToken);
        await dbContext.RefreshTokens
            .Where(token => token.Id == refreshToken.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.ReplacedByTokenHash, replacementTokenHash), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return response;
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.IsAuthenticated || currentUserContext.UserId is not Guid userId || currentUserContext.ClinicId is not Guid clinicId)
            return;

        var tokenHash = HashToken(refreshToken);
        var now = DateTime.UtcNow;
        await dbContext.RefreshTokens
            .Where(token => token.TokenHash == tokenHash && token.UserId == userId && token.ClinicId == clinicId && token.RevokedDate == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedDate, now), cancellationToken);
    }

    private async Task<bool> CanAuthenticateAsync(User user, CancellationToken cancellationToken)
    {
        if (!user.IsActive)
            return false;
        return await dbContext.Clinics.AsNoTracking()
            .AnyAsync(clinic => clinic.Id == user.ClinicId && clinic.IsActive, cancellationToken);
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
        var permissions = await effectivePermissionService.GetAsync(user.IsSuperUser, roleIds, cancellationToken);

        var sessionId = Guid.NewGuid();
        var expiresDate = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);
        var rawRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var accessToken = CreateAccessToken(user, identityUser, roles, permissions, sessionId, expiresDate);

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = sessionId,
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

    private string CreateAccessToken(User user, ApplicationIdentityUser identityUser, IEnumerable<string> roles, IEnumerable<string> permissions, Guid sessionId, DateTime expiresDate)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, identityUser.Id),
            new(ActorPolicies.ActorTypeClaim, ActorPolicies.ClinicActor),
            new("user_id", user.Id.ToString()),
            new("clinic_id", user.ClinicId.ToString()),
            new("session_id", sessionId.ToString()),
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
