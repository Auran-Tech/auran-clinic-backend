using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Authorization;

public sealed class PlatformActorAuthorizationHandler(
    ICurrentActor currentActor,
    AuranClinicDbContext dbContext) : AuthorizationHandler<PlatformActorRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PlatformActorRequirement requirement)
    {
        if (!currentActor.IsAuthenticated ||
            currentActor.ActorType != ActorType.Platform ||
            !currentActor.PlatformUserId.HasValue)
        {
            return;
        }

        var isActive = await dbContext.PlatformUsers.AsNoTracking()
            .AnyAsync(x => x.Id == currentActor.PlatformUserId.Value && x.IsActive);
        if (isActive)
            context.Succeed(requirement);
    }
}
