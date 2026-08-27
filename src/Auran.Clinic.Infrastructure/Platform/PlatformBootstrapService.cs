using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Features;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.Infrastructure.Platform;

public sealed class PlatformBootstrapService(
    AuranClinicDbContext dbContext,
    UserManager<ApplicationIdentityUser> userManager,
    SystemCatalogService catalogService,
    IAuditService auditService,
    IOptions<PlatformBootstrapOptions> options)
{
    private readonly PlatformBootstrapOptions _options = options.Value;

    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var permissions = await catalogService.EnsurePermissionsAsync(cancellationToken);
            await catalogService.EnsureFeaturesAsync(cancellationToken);

            var role = await dbContext.PlatformRoles
                .SingleOrDefaultAsync(x => x.Code == PlatformRoleCatalog.PlatformAdmin, cancellationToken);
            if (role is null)
            {
                role = new PlatformRole
                {
                    Id = Guid.NewGuid(),
                    Code = PlatformRoleCatalog.PlatformAdmin,
                    Name = "Platform Admin",
                    IsSystem = true
                };
                dbContext.PlatformRoles.Add(role);
            }

            var assignedPermissionIds = await dbContext.PlatformRolePermissions
                .Where(x => x.PlatformRoleId == role.Id)
                .Select(x => x.PermissionId)
                .ToListAsync(cancellationToken);
            foreach (var definition in SystemPermissionCatalog.Platform)
            {
                var permission = permissions[definition.Code];
                if (assignedPermissionIds.Contains(permission.Id))
                    continue;

                dbContext.PlatformRolePermissions.Add(new PlatformRolePermission
                {
                    Id = Guid.NewGuid(),
                    PlatformRoleId = role.Id,
                    PermissionId = permission.Id
                });
            }

            if (await dbContext.PlatformUsers.AnyAsync(cancellationToken))
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            ValidateInitialAdminConfiguration();
            var email = _options.Email.Trim().ToLowerInvariant();
            var identityUser = await userManager.FindByEmailAsync(email);
            if (identityUser is not null)
            {
                throw new InvalidOperationException(
                    "Platform bootstrap email is already used by another Identity account.");
            }

            identityUser = new ApplicationIdentityUser
            {
                UserName = email,
                Email = email,
                PhoneNumber = Clean(_options.Phone),
                EmailConfirmed = true,
                AccountType = AccountType.Platform
            };

            var identityResult = await userManager.CreateAsync(identityUser, _options.Password);
            if (!identityResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Platform Admin bootstrap failed: " +
                    string.Join("; ", identityResult.Errors.Select(x => x.Description)));
            }

            var platformUser = new PlatformUser
            {
                Id = Guid.NewGuid(),
                IdentityUserId = identityUser.Id,
                FullName = _options.FullName.Trim(),
                Email = email,
                Phone = Clean(_options.Phone),
                IsActive = true
            };
            dbContext.PlatformUsers.Add(platformUser);
            dbContext.PlatformUserRoles.Add(new PlatformUserRole
            {
                Id = Guid.NewGuid(),
                PlatformUserId = platformUser.Id,
                PlatformRoleId = role.Id
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await auditService.WriteAsync(new AuditEvent
            {
                Scope = AuditScope.Platform,
                ActorType = ActorType.System,
                Action = "Platform.InitialAdminBootstrapped",
                Category = "Platform",
                EntityType = nameof(PlatformUser),
                EntityId = platformUser.Id.ToString(),
                Description = "The initial AURAN Platform Admin account was created from secure deployment configuration.",
                Metadata = new { platformUser.Email, Role = role.Code }
            }, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private void ValidateInitialAdminConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.FullName) ||
            string.IsNullOrWhiteSpace(_options.Email) ||
            string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException(
                "PlatformBootstrap FullName, Email and Password are required when bootstrap is enabled and no Platform user exists.");
        }
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
