using Auran.Clinic.Application.Authorization;

namespace Auran.Clinic.UnitTests;

public sealed class SystemRoleCatalogTests
{
    [Fact]
    public void Catalog_ContainsExactlyProtectedV1Roles()
    {
        var codes = SystemRoleCatalog.All.Select(role => role.Code).OrderBy(code => code).ToArray();

        Assert.Equal(
            new[]
            {
                SystemRoleCatalog.Admin,
                SystemRoleCatalog.Doctor,
                SystemRoleCatalog.Nurse,
                SystemRoleCatalog.Receptionist
            }.OrderBy(code => code),
            codes);
    }

    [Fact]
    public void AdminRole_ContainsEveryKnownPermission()
    {
        var admin = Assert.Single(SystemRoleCatalog.All, role => role.Code == SystemRoleCatalog.Admin);
        var expected = SystemPermissionCatalog.All.Select(permission => permission.Key).OrderBy(code => code);

        Assert.Equal(expected, admin.Permissions.OrderBy(code => code));
    }

    [Fact]
    public void RolePermissions_ReferenceOnlyKnownPermissionKeys()
    {
        var known = SystemPermissionCatalog.All.Select(permission => permission.Key).ToHashSet(StringComparer.Ordinal);

        foreach (var role in SystemRoleCatalog.All)
            Assert.All(role.Permissions, permission => Assert.Contains(permission, known));
    }
}
