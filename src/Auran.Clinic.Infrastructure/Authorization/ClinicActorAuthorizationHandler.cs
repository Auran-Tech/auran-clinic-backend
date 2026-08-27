using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Features;
using Auran.Clinic.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Auran.Clinic.Infrastructure.Authorization;

public sealed class ClinicActorAuthorizationHandler(
    ICurrentActor currentActor,
    IClinicAccessService clinicAccessService) : AuthorizationHandler<ClinicActorRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ClinicActorRequirement requirement)
    {
        if (!currentActor.IsAuthenticated ||
            currentActor.ActorType != ActorType.Clinic ||
            !currentActor.ClinicId.HasValue ||
            !currentActor.ClinicUserId.HasValue)
        {
            return;
        }

        if (await clinicAccessService.IsClinicActiveAsync(currentActor.ClinicId.Value))
            context.Succeed(requirement);
    }
}
