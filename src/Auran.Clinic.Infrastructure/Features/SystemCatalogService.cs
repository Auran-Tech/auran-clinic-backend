using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Features;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using AppFeatureDefinition = Auran.Clinic.Application.Features.FeatureDefinition;
using DomainFeatureDefinition = Auran.Clinic.Domain.Entities.FeatureDefinition;

namespace Auran.Clinic.Infrastructure.Features;

public sealed class SystemCatalogService(AuranClinicDbContext dbContext)
{
    public async Task<Dictionary<string, Permission>> EnsurePermissionsAsync(
        CancellationToken cancellationToken = default)
    {
        var keys = SystemPermissionCatalog.All.Select(x => x.Key).ToArray();
        var existing = await dbContext.Permissions
            .Where(x => keys.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, StringComparer.Ordinal, cancellationToken);

        foreach (var definition in SystemPermissionCatalog.All)
        {
            if (!existing.TryGetValue(definition.Key, out var permission))
            {
                permission = new Permission
                {
                    Id = Guid.NewGuid(),
                    Code = definition.Key,
                    Name = definition.Key,
                    Group = definition.Group,
                    Scope = definition.Scope
                };
                dbContext.Permissions.Add(permission);
                existing[definition.Key] = permission;
            }
            else
            {
                permission.Name = definition.Key;
                permission.Group = definition.Group;
                permission.Scope = definition.Scope;
            }

            await UpsertTranslationAsync(permission.Id, "en", definition.EnglishDescription, cancellationToken);
            await UpsertTranslationAsync(permission.Id, "ar", definition.ArabicDescription, cancellationToken);
        }

        return existing;
    }

    public async Task<Dictionary<string, DomainFeatureDefinition>> EnsureFeaturesAsync(
        CancellationToken cancellationToken = default)
    {
        var codes = SystemFeatureCatalog.All.Select(x => x.Code).ToArray();
        var existing = await dbContext.FeatureDefinitions
            .Where(x => codes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, StringComparer.Ordinal, cancellationToken);

        foreach (AppFeatureDefinition definition in SystemFeatureCatalog.All)
        {
            if (existing.TryGetValue(definition.Code, out var feature))
            {
                feature.Name = definition.Name;
                feature.Description = definition.Description;
                feature.IsDefaultEnabled = definition.IsDefaultEnabled;
                continue;
            }

            feature = new DomainFeatureDefinition
            {
                Id = Guid.NewGuid(),
                Code = definition.Code,
                Name = definition.Name,
                Description = definition.Description,
                IsDefaultEnabled = definition.IsDefaultEnabled
            };
            dbContext.FeatureDefinitions.Add(feature);
            existing[definition.Code] = feature;
        }

        return existing;
    }

    private async Task UpsertTranslationAsync(
        Guid permissionId,
        string languageCode,
        string description,
        CancellationToken cancellationToken)
    {
        var translation = await dbContext.PermissionTranslations
            .SingleOrDefaultAsync(
                x => x.PermissionId == permissionId && x.LanguageCode == languageCode,
                cancellationToken);

        if (translation is null)
        {
            dbContext.PermissionTranslations.Add(new PermissionTranslation
            {
                Id = Guid.NewGuid(),
                PermissionId = permissionId,
                LanguageCode = languageCode,
                Description = description
            });
            return;
        }

        translation.Description = description;
    }
}
