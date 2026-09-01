using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Users;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Users;

public sealed class UserAccountService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext) : IUserAccountService
{
    public async Task<UserAccountStatusResult> SetStatusAsync(
        UpdateUserStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var currentUserId))
            return new UserAccountStatusResult(UserAccountStatusOutcome.Unauthenticated);

        var target = await dbContext.Users
            .SingleOrDefaultAsync(user => user.Id == request.UserId, cancellationToken);
        if (target is null)
            return new UserAccountStatusResult(UserAccountStatusOutcome.NotFound);

        if (target.IsSuperUser && target.Id != currentUserId && !currentUserContext.IsSuperUser)
            return new UserAccountStatusResult(UserAccountStatusOutcome.Forbidden);

        return await ApplyStatusAsync(target, request.IsActive, currentUserId, cancellationToken);
    }

    public async Task<UserAccountStatusResult> DisableCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var currentUserId))
            return new UserAccountStatusResult(UserAccountStatusOutcome.Unauthenticated);

        var currentUser = await dbContext.Users
            .SingleOrDefaultAsync(user => user.Id == currentUserId, cancellationToken);
        if (currentUser is null)
            return new UserAccountStatusResult(UserAccountStatusOutcome.NotFound);

        return await ApplyStatusAsync(currentUser, isActive: false, currentUserId, cancellationToken);
    }

    private async Task<UserAccountStatusResult> ApplyStatusAsync(
        User user,
        bool isActive,
        Guid changedByUserId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        user.IsActive = isActive;
        user.UpdatedDate = now;
        user.UpdatedByUserId = changedByUserId;

        if (!isActive)
        {
            await dbContext.RefreshTokens
                .Where(token => token.UserId == user.Id && token.RevokedDate == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(token => token.RevokedDate, now),
                    cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new UserAccountStatusResult(
            UserAccountStatusOutcome.Success,
            new UserAccountStatusResponse(user.Id, user.IsActive));
    }

    private bool TryGetCurrentActor(out Guid userId)
    {
        if (currentUserContext.IsAuthenticated &&
            currentUserContext.UserId is Guid currentUserId &&
            currentUserContext.ClinicId.HasValue)
        {
            userId = currentUserId;
            return true;
        }

        userId = Guid.Empty;
        return false;
    }
}
