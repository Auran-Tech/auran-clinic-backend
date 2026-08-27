using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Features;
using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.UnitTests;

public class FoundationTests
{
    [Fact]
    public void PermissionCatalog_HasUniqueCodesAndExplicitScopes()
    {
        var permissions = SystemPermissionCatalog.All.ToList();

        Assert.Equal(permissions.Count, permissions.Select(x => x.Code).Distinct(StringComparer.Ordinal).Count());
        Assert.All(permissions, permission => Assert.True(Enum.IsDefined(permission.Scope)));
        Assert.Contains(permissions, x => x.Scope == PermissionScope.Platform);
        Assert.Contains(permissions, x => x.Scope == PermissionScope.Clinic);
    }

    [Fact]
    public void ClinicSystemRoles_ReferenceOnlyClinicPermissions()
    {
        var clinicPermissions = SystemPermissionCatalog.Clinic.Select(x => x.Code).ToHashSet(StringComparer.Ordinal);

        Assert.All(
            SystemRoleCatalog.All.SelectMany(x => x.Permissions),
            permission => Assert.Contains(permission, clinicPermissions));
    }

    [Fact]
    public void PlatformAdmin_ReferencesOnlyPlatformPermissions()
    {
        var platformPermissions = SystemPermissionCatalog.Platform.Select(x => x.Code).ToHashSet(StringComparer.Ordinal);
        var admin = Assert.Single(PlatformRoleCatalog.All);

        Assert.Equal(PlatformRoleCatalog.PlatformAdmin, admin.Code);
        Assert.NotEmpty(admin.Permissions);
        Assert.All(admin.Permissions, permission => Assert.Contains(permission, platformPermissions));
    }

    [Fact]
    public void FeatureCatalog_HasUniqueCodes()
    {
        var features = SystemFeatureCatalog.All.ToList();

        Assert.NotEmpty(features);
        Assert.Equal(features.Count, features.Select(x => x.Code).Distinct(StringComparer.Ordinal).Count());
    }
}
