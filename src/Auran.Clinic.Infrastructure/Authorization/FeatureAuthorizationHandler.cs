using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Features;
using Auran.Clinic.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Auran.Clinic.Infrastructure.Authorization;

public sealed class FeatureAuthorizationHandler(
    ICurrentActor currentActor,
    IClinicAccessService clinicAccessService)
    : AuthorizationHandler<FeatureRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FeatureRequirement requirement)
    {
        if (!currentActor.IsAuthenticated ||
            currentActor.ActorType != ActorType.Clinic ||
            !currentActor.ClinicId.HasValue)
        {
            return;
        }

        var clinicId = currentActor.ClinicId.Value;
        if (!await clinicAccessService.IsClinicActiveAsync(clinicId))
            return;

        if (await clinicAccessService.IsFeatureEnabledAsync(clinicId, requirement.FeatureCode))
            context.Succeed(requirement);
    }
}
