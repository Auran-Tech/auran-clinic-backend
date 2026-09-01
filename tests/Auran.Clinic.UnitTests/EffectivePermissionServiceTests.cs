using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Authorization;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.UnitTests;

public sealed class EffectivePermissionServiceTests
{
    [Fact]
    public async Task SuperUser_ReturnsOnlyKnownBackendPermissionsWithoutRoleAssignments()
    {
        await using var context = CreateContext();
        context.Permissions.AddRange(
            CreatePermission(Permissions.Patients.View),
            CreatePermission(Permissions.Users.ManageStatus),
            CreatePermission("Legacy_Unknown_Permission"));
        await context.SaveChangesAsync();

        var service = new EffectivePermissionService(context);
        var permissions = await service.GetAsync(
            isSuperUser: true,
            roleIds: Array.Empty<Guid>());

        Assert.Equal(
            [Permissions.Patients.View, Permissions.Users.ManageStatus],
            permissions);
    }

    [Fact]
    public async Task RegularUser_ReturnsDistinctKnownPermissionsFromAssignedRolesOnly()
    {
        await using var context = CreateContext();
        var patientView = CreatePermission(Permissions.Patients.View);
        var userManage = CreatePermission(Permissions.Users.Manage);
        var settingsManage = CreatePermission(Permissions.Settings.Manage);
        var unknown = CreatePermission("Legacy_Unknown_Permission");
        var roleA = Guid.NewGuid();
        var roleB = Guid.NewGuid();
        var unassignedRole = Guid.NewGuid();

        context.Permissions.AddRange(patientView, userManage, settingsManage, unknown);
        context.RolePermissions.AddRange(
            new RolePermission { RoleId = roleA, PermissionId = patientView.Id },
            new RolePermission { RoleId = roleA, PermissionId = userManage.Id },
            new RolePermission { RoleId = roleA, PermissionId = unknown.Id },
            new RolePermission { RoleId = roleB, PermissionId = patientView.Id },
            new RolePermission { RoleId = unassignedRole, PermissionId = settingsManage.Id });
        await context.SaveChangesAsync();

        var service = new EffectivePermissionService(context);
        var permissions = await service.GetAsync(
            isSuperUser: false,
            roleIds: [roleA, roleB]);

        Assert.Equal([Permissions.Patients.View, Permissions.Users.Manage], permissions);
    }

    [Fact]
    public async Task RegularUserWithoutRoles_ReturnsNoPermissions()
    {
        await using var context = CreateContext();
        context.Permissions.Add(CreatePermission(Permissions.Patients.View));
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
