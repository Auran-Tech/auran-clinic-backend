using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Users;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Identity;

public sealed class UserAccountService(
    AuranClinicDbContext dbContext,
    ICurrentActor currentActor) : IUserAccountService
{
    public async Task<UserAccountStatusResult> SetStatusAsync(
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetClinicActor(out var currentUserId, out var clinicId))
            return UserAccountStatusResult.Failed("UNAUTHORIZED");

        var targetUser = await dbContext.Users.SingleOrDefaultAsync(
            x => x.Id == userId && x.ClinicId == clinicId,
            cancellationToken);
        if (targetUser is null)
            return UserAccountStatusResult.Failed("NOT_FOUND");

        if (targetUser.IsClinicSuperUser
            && targetUser.Id != currentUserId
            && !currentActor.IsClinicSuperUser)
        {
            return UserAccountStatusResult.Failed("SUPER_USER_PROTECTED");
        }

        if (targetUser.IsActive == isActive)
            return UserAccountStatusResult.Succeeded();

        targetUser.IsActive = isActive;
        if (!isActive)
            await RevokeRefreshTokensAsync(targetUser.Id, clinicId, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return UserAccountStatusResult.Succeeded();
    }

    public async Task<UserAccountStatusResult> DisableCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        if (!TryGetClinicActor(out var currentUserId, out var clinicId))
            return UserAccountStatusResult.Failed("UNAUTHORIZED");

        var currentUser = await dbContext.Users.SingleOrDefaultAsync(
            x => x.Id == currentUserId && x.ClinicId == clinicId,
            cancellationToken);
        if (currentUser is null)
            return UserAccountStatusResult.Failed("NOT_FOUND");

        currentUser.IsActive = false;
        await RevokeRefreshTokensAsync(currentUser.Id, clinicId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return UserAccountStatusResult.Succeeded();
    }

    private bool TryGetClinicActor(out Guid userId, out Guid clinicId)
    {
        userId = default;
        clinicId = default;

        if (!currentActor.IsAuthenticated
            || currentActor.ActorType != ActorType.Clinic
            || currentActor.ClinicUserId is not Guid resolvedUserId
            || currentActor.ClinicId is not Guid resolvedClinicId)
        {
            return false;
        }

        userId = resolvedUserId;
        clinicId = resolvedClinicId;
        return true;
    }

    private async Task RevokeRefreshTokensAsync(
        Guid userId,
        Guid clinicId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await dbContext.RefreshTokens
            .Where(x => x.UserId == userId && x.ClinicId == clinicId && x.RevokedDate == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RevokedDate, now), cancellationToken);
    }
}
