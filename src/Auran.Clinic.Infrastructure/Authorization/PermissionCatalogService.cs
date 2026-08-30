using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Authorization;

public sealed class PermissionCatalogService(
    AuranClinicDbContext dbContext,
    ICurrentActor currentActor) : IPermissionCatalogService
{
    public async Task<IReadOnlyList<PermissionCatalogResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var scope = currentActor.ActorType == ActorType.Platform
            ? PermissionScope.Platform
            : PermissionScope.Clinic;

        var permissions = await dbContext.Permissions.AsNoTracking()
            .Where(x => x.Scope == scope)
            .OrderBy(x => x.Group)
            .ThenBy(x => x.Code)
            .Select(x => new { x.Id, x.Code, x.Group, x.Scope })
            .ToListAsync(cancellationToken);

        var ids = permissions.Select(x => x.Id).ToArray();
        var translations = await dbContext.PermissionTranslations.AsNoTracking()
            .Where(x => ids.Contains(x.PermissionId))
            .OrderBy(x => x.LanguageCode)
            .ToListAsync(cancellationToken);

        var descriptions = translations
            .GroupBy(x => x.PermissionId)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(
                    item => item.LanguageCode,
                    item => item.Description,
                    StringComparer.OrdinalIgnoreCase));

        return permissions.Select(permission => new PermissionCatalogResponse
        {
            Key = permission.Code,
            Group = permission.Group,
            Scope = permission.Scope,
            Descriptions = descriptions.GetValueOrDefault(permission.Id)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        }).ToArray();
    }
}
