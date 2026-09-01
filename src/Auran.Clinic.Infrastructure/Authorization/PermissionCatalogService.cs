using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Authorization;

public sealed class PermissionCatalogService(AuranClinicDbContext dbContext) : IPermissionCatalogService
{
    public async Task<List<PermissionCatalogResponse>> GetAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await dbContext.Permissions
            .AsNoTracking()
            .OrderBy(permission => permission.Group)
            .ThenBy(permission => permission.Code)
            .Select(permission => new
            {
                permission.Id,
                permission.Code,
                permission.Group
            })
            .ToListAsync(cancellationToken);

        var permissionIds = permissions.Select(permission => permission.Id).ToArray();
        var translations = permissionIds.Length == 0
            ? new List<PermissionTranslationProjection>()
            : await dbContext.PermissionTranslations
                .AsNoTracking()
                .Where(translation => permissionIds.Contains(translation.PermissionId))
                .OrderBy(translation => translation.LanguageCode)
                .Select(translation => new PermissionTranslationProjection(
                    translation.PermissionId,
                    translation.LanguageCode,
                    translation.Description))
                .ToListAsync(cancellationToken);

        return permissions
            .Select(permission => new PermissionCatalogResponse
            {
                Key = permission.Code,
                Group = permission.Group,
                Descriptions = translations
                    .Where(translation => translation.PermissionId == permission.Id)
                    .ToDictionary(
                        translation => translation.LanguageCode,
                        translation => translation.Description,
                        StringComparer.OrdinalIgnoreCase)
            })
            .ToList();
    }

    private sealed record PermissionTranslationProjection(
        Guid PermissionId,
        string LanguageCode,
        string Description);
}
