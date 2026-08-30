using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Authorization;

public sealed class PermissionCatalogService(AuranClinicDbContext dbContext) : IPermissionCatalogService
{
    public async Task<IReadOnlyList<PermissionCatalogResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var permissionKeysById = await dbContext.Permissions
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Key, cancellationToken);

        var storedTranslations = await dbContext.PermissionTranslations
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var translationsByKey = storedTranslations
            .Where(x => permissionKeysById.ContainsKey(x.PermissionId))
            .GroupBy(x => permissionKeysById[x.PermissionId], StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(
                    item => item.LanguageCode,
                    item => item.Description,
                    StringComparer.OrdinalIgnoreCase),
                StringComparer.Ordinal);

        return Permissions.All
            .Select(definition =>
            {
                var descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["en"] = definition.EnglishDescription,
                    ["ar"] = definition.ArabicDescription
                };

                if (translationsByKey.TryGetValue(definition.Key, out var persisted))
                {
                    foreach (var translation in persisted)
                        descriptions[translation.Key] = translation.Value;
                }

                return new PermissionCatalogResponse
                {
                    Key = definition.Key,
                    GroupKey = definition.GroupKey,
                    Descriptions = descriptions
                };
            })
            .ToList();
    }
}
