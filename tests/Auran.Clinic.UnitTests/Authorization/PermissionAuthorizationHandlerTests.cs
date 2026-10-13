using System.Security.Claims;
using Auran.Clinic.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Auran.Clinic.UnitTests.Authorization;

public sealed class PermissionAuthorizationHandlerTests
{
    private readonly PermissionAuthorizationHandler _handler = new();

    [Theory]
    [InlineData("Visit_View", "Visit_Edit")]
    [InlineData("Patient_View", "Patient_Edit_Basic")]
    [InlineData("Settings_View", "Settings_Manage")]
    public async Task Unrelated_permission_does_not_grant_access(
        string providedPermission,
        string requiredPermission)
    {
        var requirement = new PermissionRequirement(requiredPermission);
        var context = CreateContext(requirement, providedPermission);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Missing_permission_does_not_grant_access()
    {
        var requirement = new PermissionRequirement("Visit_View");
        var context = CreateContext(requirement);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Exact_permission_grants_access()
    {
        var requirement = new PermissionRequirement("Visit_View");
        var context = CreateContext(requirement, "Visit_View");

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Permission_comparison_is_case_sensitive()
    {
        var requirement = new PermissionRequirement("Visit_View");
        var context = CreateContext(requirement, "visit_view");

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static AuthorizationHandlerContext CreateContext(
        PermissionRequirement requirement,
        params string[] permissions)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        return new AuthorizationHandlerContext(new[] { requirement }, principal, resource: null);
    }
}
