using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Authorization;

public sealed class EffectivePermissionService(AuranClinicDbContext dbContext) : IEffectivePermissionService
{
    public async Task<IReadOnlyList<string>> GetAsync(
        bool isSuperUser,
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken = default)
    {
        if (isSuperUser)
        {
            return await dbContext.Permissions
                .AsNoTracking()
                .Select(permission => permission.Code)
                .Distinct()
                .OrderBy(code => code)
                .ToListAsync(cancellationToken);
        }

        if (roleIds.Count == 0)
            return Array.Empty<string>();

        return await (
                from rolePermission in dbContext.RolePermissions.AsNoTracking()
                join permission in dbContext.Permissions.AsNoTracking()
                    on rolePermission.PermissionId equals permission.Id
                where roleIds.Contains(rolePermission.RoleId)
                select permission.Code)
            .Distinct()
            .OrderBy(code => code)
            .ToListAsync(cancellationToken);
    }
}
