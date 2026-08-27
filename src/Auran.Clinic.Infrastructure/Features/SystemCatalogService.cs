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
        var codes = SystemPermissionCatalog.All.Select(x => x.Code).ToArray();
        var existing = await dbContext.Permissions
            .Where(x => codes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, StringComparer.Ordinal, cancellationToken);

        foreach (var definition in SystemPermissionCatalog.All)
        {
            if (existing.TryGetValue(definition.Code, out var permission))
            {
                permission.Name = definition.Name;
                permission.Group = definition.Group;
                permission.Scope = definition.Scope;
                continue;
            }

            permission = new Permission
            {
                Id = Guid.NewGuid(),
                Code = definition.Code,
                Name = definition.Name,
                Group = definition.Group,
                Scope = definition.Scope
            };
            dbContext.Permissions.Add(permission);
            existing[definition.Code] = permission;
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
}
