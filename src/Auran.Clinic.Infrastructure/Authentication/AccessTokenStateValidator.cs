using System.Security.Claims;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Authentication;

public sealed class AccessTokenStateValidator(AuranClinicDbContext dbContext)
{
    public Task<bool> IsActiveAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var actorType = principal.FindFirstValue("actor_type");
        if (string.Equals(actorType, ActorType.Platform.ToString(), StringComparison.Ordinal))
            return IsPlatformSessionActiveAsync(principal, cancellationToken);

        // Tokens issued before actor_type was introduced are clinic sessions and remain valid
        // until their normal access-token expiry.
        if (actorType is null ||
            string.Equals(actorType, ActorType.Clinic.ToString(), StringComparison.Ordinal))
        {
            return IsClinicSessionActiveAsync(principal, cancellationToken);
        }

        return Task.FromResult(false);
    }

    private async Task<bool> IsClinicSessionActiveAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirstValue("user_id"), out var userId) ||
            !Guid.TryParse(principal.FindFirstValue("clinic_id"), out var clinicId) ||
            !Guid.TryParse(principal.FindFirstValue("session_id"), out var sessionId))
        {
            return false;
        }

        var userIsActive = await dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(
                user =>
                    user.Id == userId &&
                    user.ClinicId == clinicId &&
                    user.IsActive,
                cancellationToken);
        if (!userIsActive)
            return false;

        var clinicIsActive = await dbContext.Clinics.AsNoTracking()
            .AnyAsync(
                clinic => clinic.Id == clinicId && clinic.IsActive,
                cancellationToken);
        if (!clinicIsActive)
            return false;

        var now = DateTime.UtcNow;
        return await dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(
                token =>
                    token.Id == sessionId &&
                    token.UserId == userId &&
                    token.ClinicId == clinicId &&
                    token.RevokedDate == null &&
                    token.ExpiresDate > now,
                cancellationToken);
    }

    private async Task<bool> IsPlatformSessionActiveAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(principal.FindFirstValue("platform_user_id"), out var platformUserId) ||
            !Guid.TryParse(principal.FindFirstValue("session_id"), out var sessionId))
        {
            return false;
        }

        var userIsActive = await dbContext.PlatformUsers.AsNoTracking()
            .AnyAsync(
                user => user.Id == platformUserId && user.IsActive,
                cancellationToken);
        if (!userIsActive)
            return false;

        var now = DateTime.UtcNow;
        return await dbContext.PlatformRefreshTokens.AsNoTracking()
            .AnyAsync(
                token =>
                    token.Id == sessionId &&
                    token.PlatformUserId == platformUserId &&
                    token.RevokedDate == null &&
                    token.ExpiresDate > now,
                cancellationToken);
    }
}
