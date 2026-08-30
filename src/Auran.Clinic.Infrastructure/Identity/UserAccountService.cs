using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Users;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Identity;

public sealed class UserAccountService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext) : IUserAccountService
{
    public async Task<UserAccountStatusResult> SetStatusAsync(
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.IsAuthenticated ||
            currentUserContext.UserId is not Guid currentUserId ||
            currentUserContext.ClinicId is not Guid clinicId)
        {
            return UserAccountStatusResult.Failed("UNAUTHORIZED");
        }

        var targetUser = await dbContext.Users.SingleOrDefaultAsync(
            x => x.Id == userId && x.ClinicId == clinicId,
            cancellationToken);

        if (targetUser is null)
            return UserAccountStatusResult.Failed("NOT_FOUND");

        if (targetUser.IsSuperUser &&
            targetUser.Id != currentUserId &&
            !currentUserContext.IsSuperUser)
        {
            return UserAccountStatusResult.Failed("SUPER_USER_PROTECTED");
        }

        targetUser.IsActive = isActive;
        if (!isActive)
            await RevokeRefreshTokensAsync(targetUser.Id, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return UserAccountStatusResult.Succeeded();
    }

    public async Task<UserAccountStatusResult> DisableCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.IsAuthenticated ||
            currentUserContext.UserId is not Guid currentUserId ||
            currentUserContext.ClinicId is not Guid clinicId)
        {
            return UserAccountStatusResult.Failed("UNAUTHORIZED");
        }

        var currentUser = await dbContext.Users.SingleOrDefaultAsync(
            x => x.Id == currentUserId && x.ClinicId == clinicId,
            cancellationToken);

        if (currentUser is null)
            return UserAccountStatusResult.Failed("NOT_FOUND");

        currentUser.IsActive = false;
        await RevokeRefreshTokensAsync(currentUser.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return UserAccountStatusResult.Succeeded();
    }

    private async Task RevokeRefreshTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedDate == null)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var token in activeTokens)
            token.RevokedDate = now;
    }
}
