using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Features;
using Auran.Clinic.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.Infrastructure.Authorization;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PermissionPolicy.ClinicPrefix, StringComparison.Ordinal))
        {
            var permission = policyName[PermissionPolicy.ClinicPrefix.Length..];
            return Build(new PermissionRequirement(permission, PermissionScope.Clinic));
        }

        if (policyName.StartsWith(PermissionPolicy.PlatformPrefix, StringComparison.Ordinal))
        {
            var permission = policyName[PermissionPolicy.PlatformPrefix.Length..];
            return Build(new PermissionRequirement(permission, PermissionScope.Platform));
        }

        if (policyName.StartsWith(FeaturePolicy.Prefix, StringComparison.Ordinal))
        {
            var feature = policyName[FeaturePolicy.Prefix.Length..];
            return Build(new FeatureRequirement(feature));
        }

        return await base.GetPolicyAsync(policyName);
    }

    private static AuthorizationPolicy Build(IAuthorizationRequirement requirement) =>
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(requirement)
            .Build();
}
