using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Authorization;

public sealed class PermissionCatalogInitializer(AuranClinicDbContext dbContext)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var definitions = SystemPermissionCatalog.All;
        var knownCodes = definitions
            .SelectMany(definition => new[] { definition.Key, definition.LegacyKey })
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var permissions = await dbContext.Permissions
            .Where(permission => knownCodes.Contains(permission.Code))
            .ToListAsync(cancellationToken);

        var relevantPermissionIds = permissions.Select(permission => permission.Id).ToArray();
        var rolePermissions = relevantPermissionIds.Length == 0
            ? new List<RolePermission>()
            : await dbContext.RolePermissions
                .Where(rolePermission => relevantPermissionIds.Contains(rolePermission.PermissionId))
                .ToListAsync(cancellationToken);

        var translations = relevantPermissionIds.Length == 0
            ? new List<PermissionTranslation>()
            : await dbContext.PermissionTranslations
                .Where(translation => relevantPermissionIds.Contains(translation.PermissionId))
                .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        foreach (var definition in definitions)
        {
            var current = permissions.SingleOrDefault(permission => permission.Code == definition.Key);
            var legacy = definition.LegacyKey is null
                ? null
                : permissions.SingleOrDefault(permission => permission.Code == definition.LegacyKey);

            if (current is null && legacy is not null)
            {
                current = legacy;
                current.Code = definition.Key;
            }
            else if (current is not null && legacy is not null && current.Id != legacy.Id)
            {
                MergeLegacyPermission(current, legacy, rolePermissions, translations);
                permissions.Remove(legacy);
            }

            if (current is null)
            {
                current = new Permission
                {
                    Id = Guid.NewGuid(),
                    Code = definition.Key,
                    Name = definition.Key,
                    Group = definition.Group,
                    CreatedDate = now
                };

                dbContext.Permissions.Add(current);
                permissions.Add(current);
            }
            else
            {
                current.Code = definition.Key;
                current.Name = definition.Key;
                current.Group = definition.Group;
                current.UpdatedDate = now;
            }

            UpsertTranslation(current, "en", definition.EnglishDescription, translations, now);
            UpsertTranslation(current, "ar", definition.ArabicDescription, translations, now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void MergeLegacyPermission(
        Permission current,
        Permission legacy,
        List<RolePermission> rolePermissions,
        List<PermissionTranslation> translations)
    {
        var currentRoleIds = rolePermissions
            .Where(rolePermission => rolePermission.PermissionId == current.Id)
            .Select(rolePermission => rolePermission.RoleId)
            .ToHashSet();

        foreach (var legacyRolePermission in rolePermissions
                     .Where(rolePermission => rolePermission.PermissionId == legacy.Id)
                     .ToList())
        {
            if (currentRoleIds.Add(legacyRolePermission.RoleId))
            {
                legacyRolePermission.PermissionId = current.Id;
                continue;
            }

            dbContext.RolePermissions.Remove(legacyRolePermission);
            rolePermissions.Remove(legacyRolePermission);
        }

        foreach (var legacyTranslation in translations
                     .Where(translation => translation.PermissionId == legacy.Id)
                     .ToList())
        {
            var existing = translations.SingleOrDefault(translation =>
                translation.PermissionId == current.Id &&
                string.Equals(
                    translation.LanguageCode,
                    legacyTranslation.LanguageCode,
                    StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                legacyTranslation.PermissionId = current.Id;
                continue;
            }

            dbContext.PermissionTranslations.Remove(legacyTranslation);
            translations.Remove(legacyTranslation);
        }

        dbContext.Permissions.Remove(legacy);
    }

    private void UpsertTranslation(
        Permission permission,
        string languageCode,
        string description,
        List<PermissionTranslation> translations,
        DateTime now)
    {
        var translation = translations.SingleOrDefault(item =>
            item.PermissionId == permission.Id &&
            string.Equals(item.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase));

        if (translation is null)
        {
            translation = new PermissionTranslation
            {
                Id = Guid.NewGuid(),
                PermissionId = permission.Id,
                LanguageCode = languageCode,
                Description = description,
                CreatedDate = now
            };
            dbContext.PermissionTranslations.Add(translation);
            translations.Add(translation);
            return;
        }

        var changed = false;
        if (translation.LanguageCode != languageCode)
        {
            translation.LanguageCode = languageCode;
            changed = true;
        }

        if (translation.Description != description)
        {
            translation.Description = description;
            changed = true;
        }

        if (changed)
            translation.UpdatedDate = now;
    }
}
