using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Authorization;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Auran.Clinic.IntegrationTests;

public sealed class PermissionCatalogInitializationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task InitializeAsync_MergesLegacyPermissionAndPreservesRoleAssignment()
    {
        _ = factory.Services;

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var initializer = scope.ServiceProvider.GetRequiredService<PermissionCatalogInitializer>();
        var stablePermission = await dbContext.Permissions
            .SingleAsync(permission => permission.Code == Permissions.Patients.View);

        var suffix = Guid.NewGuid().ToString("N")[..10];
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Code = $"CATALOG_TEST_{suffix}",
            Name = $"Catalog Test {suffix}",
            CreatedDate = DateTime.UtcNow
        };
        var legacyPermission = new Permission
        {
            Id = Guid.NewGuid(),
            Code = "Patients.View",
            Name = "Patients.View",
            Group = "Patients",
            CreatedDate = DateTime.UtcNow
        };

        dbContext.Roles.Add(role);
        dbContext.Permissions.Add(legacyPermission);
        dbContext.RolePermissions.Add(new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = role.Id,
            PermissionId = legacyPermission.Id,
            CreatedDate = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        await initializer.InitializeAsync();
        await initializer.InitializeAsync();
        dbContext.ChangeTracker.Clear();

        Assert.False(await dbContext.Permissions.AnyAsync(permission => permission.Code == "Patients.View"));
        Assert.Equal(
            1,
            await dbContext.Permissions.CountAsync(permission => permission.Code == Permissions.Patients.View));
        Assert.True(await dbContext.RolePermissions.AnyAsync(rolePermission =>
            rolePermission.RoleId == role.Id &&
            rolePermission.PermissionId == stablePermission.Id));

        var translations = await dbContext.PermissionTranslations
            .Where(translation => translation.PermissionId == stablePermission.Id)
            .OrderBy(translation => translation.LanguageCode)
            .ToListAsync();

        Assert.Equal(2, translations.Count);
        Assert.Contains(translations, translation =>
            translation.LanguageCode == "en" &&
            translation.Description == "View patient information");
        Assert.Contains(translations, translation =>
            translation.LanguageCode == "ar" &&
            translation.Description == "عرض بيانات المرضى");
    }

    [Fact]
    public async Task HostedInitialization_SeedsEveryDefinedPermissionWithEnglishAndArabicDescriptions()
    {
        _ = factory.Services;

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var definedKeys = SystemPermissionCatalog.All.Select(definition => definition.Key).ToArray();

        var permissions = await dbContext.Permissions
            .Where(permission => definedKeys.Contains(permission.Code))
            .Select(permission => new
            {
                permission.Id,
                permission.Code
            })
            .ToListAsync();

        Assert.Equal(definedKeys.Length, permissions.Count);

        var permissionIds = permissions.Select(permission => permission.Id).ToArray();
        var translations = await dbContext.PermissionTranslations
            .Where(translation => permissionIds.Contains(translation.PermissionId))
            .ToListAsync();

        foreach (var permission in permissions)
        {
            Assert.Contains(translations, translation =>
                translation.PermissionId == permission.Id && translation.LanguageCode == "en");
            Assert.Contains(translations, translation =>
                translation.PermissionId == permission.Id && translation.LanguageCode == "ar");
        }
    }
}
