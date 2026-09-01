using System.Security.Claims;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Auran.Clinic.UnitTests;

public sealed class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task SuperUserFlagWithoutPermissionClaim_DoesNotSatisfyRequirement()
    {
        var requirement = new PermissionRequirement(Permissions.Users.ManageStatus);
        var principal = CreatePrincipal(new Claim("super_user", "true"));
        var context = new AuthorizationHandlerContext([requirement], principal, resource: null);
        var handler = new PermissionAuthorizationHandler();

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task MatchingPermissionClaim_SatisfiesRequirement()
    {
        var requirement = new PermissionRequirement(Permissions.Users.ManageStatus);
        var principal = CreatePrincipal(
            new Claim("super_user", "false"),
            new Claim("permission", Permissions.Users.ManageStatus));
        var context = new AuthorizationHandlerContext([requirement], principal, resource: null);
        var handler = new PermissionAuthorizationHandler();

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task DifferentPermissionClaim_DoesNotSatisfyRequirement()
    {
        var requirement = new PermissionRequirement(Permissions.Users.ManageStatus);
        var principal = CreatePrincipal(
            new Claim("permission", Permissions.Users.View));
        var context = new AuthorizationHandlerContext([requirement], principal, resource: null);
        var handler = new PermissionAuthorizationHandler();

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "test"));
}
