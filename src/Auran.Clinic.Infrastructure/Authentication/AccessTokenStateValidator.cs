using System.Security.Claims;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Authentication;

public sealed class AccessTokenStateValidator(AuranClinicDbContext dbContext)
{
    public async Task<bool> IsActiveAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
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
}
