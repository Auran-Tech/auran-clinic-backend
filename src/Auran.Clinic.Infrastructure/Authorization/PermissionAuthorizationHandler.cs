using Microsoft.AspNetCore.Authorization;

namespace Auran.Clinic.Infrastructure.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var isSuperUser = context.User.HasClaim("super_user", "true");
        var hasPermission = context.User.HasClaim("permission", requirement.Permission);

        if (isSuperUser || hasPermission)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
