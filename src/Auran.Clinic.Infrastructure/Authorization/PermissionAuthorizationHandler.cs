using System.Security.Claims;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Authorization;

public sealed class PermissionAuthorizationHandler(
    AuranClinicDbContext dbContext,
    IEffectivePermissionService effectivePermissionService)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!Guid.TryParse(context.User.FindFirstValue("user_id"), out var userId) ||
            !Guid.TryParse(context.User.FindFirstValue("clinic_id"), out var clinicId))
        {
            return;
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .Where(item =>
                item.Id == userId &&
                item.ClinicId == clinicId &&
                item.IsActive)
            .Select(item => new
            {
                item.Id,
                item.IsSuperUser
            })
            .SingleOrDefaultAsync();

        if (user is null)
            return;

        var roleIds = await dbContext.UserRoles
            .AsNoTracking()
            .Where(item =>
                item.ClinicId == clinicId &&
                item.UserId == user.Id)
            .Select(item => item.RoleId)
            .ToListAsync();

        var permissions = await effectivePermissionService.GetAsync(
            user.IsSuperUser,
            roleIds);

        if (permissions.Contains(requirement.Permission, StringComparer.Ordinal))
            context.Succeed(requirement);
    }
}
