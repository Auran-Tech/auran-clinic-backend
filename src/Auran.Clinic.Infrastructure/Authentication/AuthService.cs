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

public sealed class AuthService(
    AuranClinicDbContext dbContext,
    UserManager<ApplicationIdentityUser> userManager,
    SignInManager<ApplicationIdentityUser> signInManager,
    ICurrentActor currentActor,
    IOptions<JwtOptions> jwtOptions,
    IAuditService auditService) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var identityUser = await userManager.FindByEmailAsync(request.Email.Trim());
        if (identityUser is null || identityUser.AccountType != AccountType.Clinic)
            return null;

        var user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdentityUserId == identityUser.Id, cancellationToken);
        if (user is null)
            return null;

        var passwordResult = await signInManager.CheckPasswordSignInAsync(
            identityUser,
            request.Password,
            lockoutOnFailure: true);

        if (!passwordResult.Succeeded)
        {
            await WriteAuthAuditAsync(user, identityUser, "Authentication.LoginFailed",
                passwordResult.IsLockedOut
                    ? "Login was blocked because the Identity account is temporarily locked."
                    : "Login failed because the supplied credentials were invalid.",
                cancellationToken);
            return null;
        }

        if (!user.IsActive)
        {
            await WriteAuthAuditAsync(user, identityUser, "Authentication.LoginBlocked",
                "Login was blocked because the clinic user account is inactive.", cancellationToken);
            return null;
        }

        var clinicIsActive = await dbContext.Clinics.AsNoTracking()
            .Where(x => x.Id == user.ClinicId)
            .Select(x => x.IsActive)
            .SingleOrDefaultAsync(cancellationToken);

        if (!clinicIsActive)
        {
            await WriteAuthAuditAsync(user, identityUser, "Authentication.LoginBlocked",
                "Login was blocked because the clinic is inactive.", cancellationToken);
            return null;
        }

        var response = await CreateSessionAsync(user, identityUser, cancellationToken);
        await WriteAuthAuditAsync(user, identityUser, "Authentication.LoginSucceeded",
            "Clinic user signed in successfully.", cancellationToken);

        return response;
    }

    public async Task<AuthResponse?> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return null;

        var tokenHash = HashToken(request.RefreshToken.Trim());
        var now = DateTime.UtcNow;

        var refreshToken = await dbContext.RefreshTokens.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (refreshToken is null || refreshToken.RevokedDate is not null || refreshToken.ExpiresDate <= now)
            return null;

        var clinicIsActive = await dbContext.Clinics.AsNoTracking()
            .Where(x => x.Id == refreshToken.ClinicId)
            .Select(x => x.IsActive)
            .SingleOrDefaultAsync(cancellationToken);
        if (!clinicIsActive)
            return null;

        var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == refreshToken.UserId && x.ClinicId == refreshToken.ClinicId,
            cancellationToken);
        if (user is null || !user.IsActive)
            return null;

        var identityUser = await userManager.FindByIdAsync(user.IdentityUserId);
        if (identityUser is null
            || identityUser.AccountType != AccountType.Clinic
            || await userManager.IsLockedOutAsync(identityUser))
        {
            return null;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var revokedRows = await dbContext.RefreshTokens
                .Where(x => x.Id == refreshToken.Id && x.RevokedDate == null && x.ExpiresDate > now)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.RevokedDate, now),
                    cancellationToken);

            if (revokedRows != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var response = await CreateSessionAsync(user, identityUser, cancellationToken, saveChanges: false);
            await dbContext.SaveChangesAsync(cancellationToken);

            var replacementHash = HashToken(response.RefreshToken);
            await dbContext.RefreshTokens
                .Where(x => x.Id == refreshToken.Id)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.ReplacedByTokenHash, replacementHash),
                    cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            await WriteAuthAuditAsync(user, identityUser, "Authentication.TokenRefreshed",
                "Clinic access and refresh tokens were rotated successfully.", cancellationToken);

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
        if (string.IsNullOrWhiteSpace(refreshToken)
            || currentActor.ActorType != ActorType.Clinic
            || currentActor.ClinicUserId is not Guid currentUserId
            || currentActor.ClinicId is not Guid currentClinicId)
        {
            return;
        }

        var tokenHash = HashToken(refreshToken.Trim());
        var entity = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (entity is null
            || entity.RevokedDate is not null
            || entity.UserId != currentUserId
            || entity.ClinicId != currentClinicId)
        {
            return;
        }

        entity.RevokedDate = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var user = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == entity.UserId, cancellationToken);

        if (user is not null)
        {
            var identityUser = await userManager.FindByIdAsync(user.IdentityUserId);
            if (identityUser is not null)
            {
                await WriteAuthAuditAsync(user, identityUser, "Authentication.Logout",
                    "Clinic authentication session was revoked during logout.", cancellationToken);
            }
        }
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

        var permissions = user.IsClinicSuperUser
            ? await dbContext.Permissions.AsNoTracking()
                .Where(x => x.Scope == PermissionScope.Clinic)
                .Select(x => x.Code)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(cancellationToken)
            : await (from rolePermission in dbContext.RolePermissions.AsNoTracking()
                     join permission in dbContext.Permissions.AsNoTracking()
                         on rolePermission.PermissionId equals permission.Id
                     where rolePermission.ClinicId == user.ClinicId
                           && roleIds.Contains(rolePermission.RoleId)
                           && permission.Scope == PermissionScope.Clinic
                     select permission.Code)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var sessionId = Guid.NewGuid();
        var expiresDate = now.AddMinutes(_jwt.AccessTokenMinutes);
        var accessToken = CreateAccessToken(user, identityUser, roles, permissions, sessionId, expiresDate);
        var rawRefreshToken = CreateRefreshToken();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = sessionId,
            ClinicId = user.ClinicId,
            UserId = user.Id,
            TokenHash = HashToken(rawRefreshToken),
            ExpiresDate = now.AddDays(_jwt.RefreshTokenDays),
            CreatedDate = now
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
                IsClinicSuperUser = user.IsClinicSuperUser,
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
        Guid sessionId,
        DateTime expiresDate)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, identityUser.Id),
            new(ClaimTypes.NameIdentifier, identityUser.Id),
            new("actor_type", ActorType.Clinic.ToString()),
            new("clinic_user_id", user.Id.ToString()),
            new("clinic_id", user.ClinicId.ToString()),
            new("clinic_super_user", user.IsClinicSuperUser.ToString().ToLowerInvariant()),
            new("session_id", sessionId.ToString()),
            new("display_name", user.FullName),
            new(ClaimTypes.Email, identityUser.Email ?? user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim("clinic_role", role)));
        claims.AddRange(permissions.Select(permission => new Claim("clinic_permission", permission)));

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

    private Task WriteAuthAuditAsync(
        User user,
        ApplicationIdentityUser identityUser,
        string action,
        string description,
        CancellationToken cancellationToken) =>
        auditService.WriteAsync(new AuditEvent
        {
            Scope = AuditScope.Clinic,
            ClinicId = user.ClinicId,
            ActorType = ActorType.Clinic,
            ActorId = user.Id,
            ActorIdentityUserId = identityUser.Id,
            ActorDisplayName = user.FullName,
            ActorEmail = identityUser.Email ?? user.Email,
            Action = action,
            Category = "Security",
            EntityType = nameof(User),
            EntityId = user.Id.ToString(),
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
