using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Features;
using Auran.Clinic.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Auran.Clinic.Infrastructure.Authorization;

public sealed class PermissionAuthorizationHandler(
    ICurrentActor currentActor,
    IClinicAccessService clinicAccessService)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!currentActor.IsAuthenticated)
            return;

        if (requirement.Scope == PermissionScope.Platform)
        {
            if (currentActor.ActorType != ActorType.Platform)
                return;

            if (context.User.HasClaim("platform_permission", requirement.Permission))
                context.Succeed(requirement);

            return;
        }

        if (currentActor.ActorType != ActorType.Clinic || !currentActor.ClinicId.HasValue)
            return;

        if (!await clinicAccessService.IsClinicActiveAsync(currentActor.ClinicId.Value))
            return;

        if (currentActor.IsClinicSuperUser ||
            context.User.HasClaim("clinic_permission", requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}
