using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Authorization;

public sealed class RoleCatalogService(AuranClinicDbContext dbContext) : IRoleCatalogService
{
    public async Task<IReadOnlyCollection<RoleCatalogResponse>> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var roleCodes = SystemRoleCatalog.All.Select(role => role.Code).ToArray();

        var roles = await dbContext.Roles.AsNoTracking()
            .Where(role => roleCodes.Contains(role.Code))
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);

        var roleIds = roles.Select(role => role.Id).ToArray();
        var mappings = await (
                from mapping in dbContext.RolePermissions.AsNoTracking()
                join permission in dbContext.Permissions.AsNoTracking()
                    on mapping.PermissionId equals permission.Id
                where roleIds.Contains(mapping.RoleId)
                select new { mapping.RoleId, permission.Code })
            .ToListAsync(cancellationToken);

        return roles
            .Select(role => new RoleCatalogResponse(
                role.Code,
                role.Name,
                mappings
                    .Where(mapping => mapping.RoleId == role.Id)
                    .Select(mapping => mapping.Code)
                    .OrderBy(code => code)
                    .ToArray()))
            .ToArray();
    }
}
