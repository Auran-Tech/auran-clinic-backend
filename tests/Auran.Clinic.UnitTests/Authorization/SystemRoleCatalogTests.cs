using Auran.Clinic.Application.Authorization;

namespace Auran.Clinic.UnitTests.Authorization;

public sealed class SystemRoleCatalogTests
{
    [Fact]
    public void V1_role_catalog_contains_expected_protected_roles()
    {
        var codes = SystemRoleCatalog.All.Select(role => role.Code).ToArray();

        Assert.Contains(SystemRoleCatalog.Admin, codes);
        Assert.Contains(SystemRoleCatalog.Receptionist, codes);
        Assert.Contains(SystemRoleCatalog.Doctor, codes);
        Assert.Contains(SystemRoleCatalog.Nurse, codes);
        Assert.Equal(4, codes.Length);
    }

    [Fact]
    public void Every_system_role_has_permissions()
    {
        Assert.All(SystemRoleCatalog.All, role => Assert.NotEmpty(role.Permissions));
    }
}
