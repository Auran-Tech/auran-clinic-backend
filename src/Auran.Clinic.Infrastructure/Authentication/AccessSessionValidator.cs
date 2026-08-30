using System.Security.Claims;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Authentication;

public sealed class AccessSessionValidator(AuranClinicDbContext dbContext)
{
    public async Task<bool> IsActiveAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(principal.FindFirstValue("session_id"), out var sessionId))
            return false;

        var actorType = principal.FindFirstValue("actor_type");
        var now = DateTime.UtcNow;

        if (string.Equals(actorType, ActorType.Platform.ToString(), StringComparison.Ordinal))
        {
            if (!Guid.TryParse(principal.FindFirstValue("platform_user_id"), out var platformUserId))
                return false;

            return await dbContext.PlatformRefreshTokens.AsNoTracking().AnyAsync(
                x => x.Id == sessionId
                     && x.PlatformUserId == platformUserId
                     && x.RevokedDate == null
                     && x.ExpiresDate > now,
                cancellationToken);
        }

        if (!string.Equals(actorType, ActorType.Clinic.ToString(), StringComparison.Ordinal)
            || !Guid.TryParse(principal.FindFirstValue("clinic_user_id"), out var clinicUserId)
            || !Guid.TryParse(principal.FindFirstValue("clinic_id"), out var clinicId))
        {
            return false;
        }

        var sessionIsActive = await dbContext.RefreshTokens.AsNoTracking().AnyAsync(
            x => x.Id == sessionId
                 && x.UserId == clinicUserId
                 && x.ClinicId == clinicId
                 && x.RevokedDate == null
                 && x.ExpiresDate > now,
            cancellationToken);
        if (!sessionIsActive)
            return false;

        var userIsActive = await dbContext.Users.AsNoTracking().AnyAsync(
            x => x.Id == clinicUserId && x.ClinicId == clinicId && x.IsActive,
            cancellationToken);
        if (!userIsActive)
            return false;

        return await dbContext.Clinics.AsNoTracking().AnyAsync(
            x => x.Id == clinicId && x.IsActive,
            cancellationToken);
    }
}
