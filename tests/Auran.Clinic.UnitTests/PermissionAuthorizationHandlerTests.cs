using System.Security.Claims;
using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Authorization;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.UnitTests;

public sealed class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task CurrentRolePermission_SatisfiesRequirementWithoutPermissionClaim()
    {
        var clinicId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var dbContext = CreateContext(clinicId);
        var roleId = await SeedUserRoleAsync(
            dbContext,
            clinicId,
            userId,
            Permissions.Users.ManageStatus,
            includePermissionMapping: true);

        var requirement = new PermissionRequirement(Permissions.Users.ManageStatus);
        var principal = CreatePrincipal(
            new Claim("user_id", userId.ToString()),
            new Claim("clinic_id", clinicId.ToString()));
        var context = new AuthorizationHandlerContext([requirement], principal, resource: null);
        var handler = new PermissionAuthorizationHandler(
            dbContext,
            new EffectivePermissionService(dbContext));

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        Assert.NotEqual(Guid.Empty, roleId);
    }

    [Fact]
    public async Task StalePermissionClaim_DoesNotSatisfyRequirementAfterCurrentPermissionIsRemoved()
    {
        var clinicId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var dbContext = CreateContext(clinicId);
        await SeedUserRoleAsync(
            dbContext,
            clinicId,
            userId,
            Permissions.Users.ManageStatus,
            includePermissionMapping: false);

        var requirement = new PermissionRequirement(Permissions.Users.ManageStatus);
        var principal = CreatePrincipal(
            new Claim("user_id", userId.ToString()),
            new Claim("clinic_id", clinicId.ToString()),
            new Claim("permission", Permissions.Users.ManageStatus));
        var context = new AuthorizationHandlerContext([requirement], principal, resource: null);
        var handler = new PermissionAuthorizationHandler(
            dbContext,
            new EffectivePermissionService(dbContext));

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task InactiveUser_DoesNotSatisfyRequirement()
    {
        var clinicId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var dbContext = CreateContext(clinicId);
        await SeedUserRoleAsync(
            dbContext,
            clinicId,
            userId,
            Permissions.Users.ManageStatus,
            includePermissionMapping: true,
            isActive: false);

        var requirement = new PermissionRequirement(Permissions.Users.ManageStatus);
        var principal = CreatePrincipal(
            new Claim("user_id", userId.ToString()),
            new Claim("clinic_id", clinicId.ToString()));
        var context = new AuthorizationHandlerContext([requirement], principal, resource: null);
        var handler = new PermissionAuthorizationHandler(
            dbContext,
            new EffectivePermissionService(dbContext));

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static async Task<Guid> SeedUserRoleAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid userId,
        string permissionCode,
        bool includePermissionMapping,
        bool isActive = true)
    {
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        dbContext.Users.Add(new User
        {
            Id = userId,
            ClinicId = clinicId,
            IdentityUserId = Guid.NewGuid().ToString(),
            FullName = "Authorization Test User",
            IsActive = isActive,
            IsSuperUser = false
        });

        dbContext.Roles.Add(new Role
        {
            Id = roleId,
            Code = "TEST_ROLE",
            Name = "Test Role"
        });

        dbContext.Permissions.Add(new Permission
        {
            Id = permissionId,
            Code = permissionCode,
            Name = permissionCode,
            Group = "Users"
        });

        dbContext.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            UserId = userId,
            RoleId = roleId
        });

        if (includePermissionMapping)
        {
            dbContext.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = roleId,
                PermissionId = permissionId
            });
        }

        await dbContext.SaveChangesAsync();
        return roleId;
    }

    private static AuranClinicDbContext CreateContext(Guid clinicId)
    {
        var options = new DbContextOptionsBuilder<AuranClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AuranClinicDbContext(
            options,
            new TestCurrentUserContext
            {
                IsAuthenticated = true,
                UserId = Guid.NewGuid(),
                ClinicId = clinicId
            });
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "test"));

    private sealed class TestCurrentUserContext : ICurrentUserContext
    {
        public bool IsAuthenticated { get; init; }
        public Guid? UserId { get; init; }
        public Guid? ClinicId { get; init; }
        public bool IsSuperUser { get; init; }
    }
}
