using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Authorization;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.UnitTests;

public sealed class EffectivePermissionServiceTests
{
    [Fact]
    public async Task SuperUser_ReturnsAllBackendPermissionsWithoutRoleAssignments()
    {
        await using var context = CreateContext();
        context.Permissions.AddRange(
            CreatePermission("Settings.Manage"),
            CreatePermission("Patients.View"),
            CreatePermission("Users.Manage"));
        await context.SaveChangesAsync();

        var service = new EffectivePermissionService(context);
        var permissions = await service.GetAsync(
            isSuperUser: true,
            roleIds: Array.Empty<Guid>());

        Assert.Equal(
            ["Patients.View", "Settings.Manage", "Users.Manage"],
            permissions);
    }

    [Fact]
    public async Task RegularUser_ReturnsDistinctPermissionsFromAssignedRolesOnly()
    {
        await using var context = CreateContext();
        var patientView = CreatePermission("Patients.View");
        var userManage = CreatePermission("Users.Manage");
        var settingsManage = CreatePermission("Settings.Manage");
        var roleA = Guid.NewGuid();
        var roleB = Guid.NewGuid();
        var unassignedRole = Guid.NewGuid();

        context.Permissions.AddRange(patientView, userManage, settingsManage);
        context.RolePermissions.AddRange(
            new RolePermission { RoleId = roleA, PermissionId = patientView.Id },
            new RolePermission { RoleId = roleA, PermissionId = userManage.Id },
            new RolePermission { RoleId = roleB, PermissionId = patientView.Id },
            new RolePermission { RoleId = unassignedRole, PermissionId = settingsManage.Id });
        await context.SaveChangesAsync();

        var service = new EffectivePermissionService(context);
        var permissions = await service.GetAsync(
            isSuperUser: false,
            roleIds: [roleA, roleB]);

        Assert.Equal(["Patients.View", "Users.Manage"], permissions);
    }

    [Fact]
    public async Task RegularUserWithoutRoles_ReturnsNoPermissions()
    {
        await using var context = CreateContext();
        context.Permissions.Add(CreatePermission("Patients.View"));
        await context.SaveChangesAsync();

        var service = new EffectivePermissionService(context);
        var permissions = await service.GetAsync(
            isSuperUser: false,
            roleIds: Array.Empty<Guid>());

        Assert.Empty(permissions);
    }

    private static AuranClinicDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AuranClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AuranClinicDbContext(options, new TestCurrentUserContext());
    }

    private static Permission CreatePermission(string code) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Name = code,
        Group = "Test"
    };

    private sealed class TestCurrentUserContext : ICurrentUserContext
    {
        public bool IsAuthenticated => false;
        public Guid? UserId => null;
        public Guid? ClinicId => null;
        public bool IsSuperUser => false;
    }
}
