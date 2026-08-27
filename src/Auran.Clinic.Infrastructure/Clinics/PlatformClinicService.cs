using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Clinics;
using Auran.Clinic.Application.Features;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Features;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Clinics;

public sealed class PlatformClinicService(
    AuranClinicDbContext dbContext,
    UserManager<ApplicationIdentityUser> userManager,
    ICurrentActor currentActor,
    IAuditService auditService,
    IClinicAccessService clinicAccessService,
    SystemCatalogService catalogService) : IPlatformClinicService
{
    public async Task<ClinicProvisioningResult> CreateAsync(
        CreateClinicRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsPlatformActor())
            return new ClinicProvisioningResult { Error = "A Platform user is required to provision a clinic." };

        var clinicCode = request.Code.Trim().ToUpperInvariant();
        var adminEmail = request.Admin.Email.Trim().ToLowerInvariant();
        if (await dbContext.Clinics.AnyAsync(x => x.Code == clinicCode, cancellationToken))
            return new ClinicProvisioningResult { Error = "Clinic code already exists.", IsConflict = true };
        if (await userManager.FindByEmailAsync(adminEmail) is not null)
            return new ClinicProvisioningResult { Error = "Admin email is already in use.", IsConflict = true };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var permissions = await catalogService.EnsurePermissionsAsync(cancellationToken);
            var features = await catalogService.EnsureFeaturesAsync(cancellationToken);

            var clinic = new Domain.Entities.Clinic
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Code = clinicCode,
                IsActive = true,
                LogoUrl = Clean(request.LogoUrl),
                PrimaryColor = Clean(request.PrimaryColor),
                SecondaryColor = Clean(request.SecondaryColor),
                FontFamily = Clean(request.FontFamily),
                WelcomeTitle = Clean(request.WelcomeTitle),
                WelcomeMessage = Clean(request.WelcomeMessage),
                TimeZoneId = Clean(request.TimeZoneId) ?? "UTC",
                PatientNumberPrefix = request.PatientNumberPrefix.Trim().ToUpperInvariant()
            };
            dbContext.Clinics.Add(clinic);

            var settings = new ClinicSettings
            {
                Id = Guid.NewGuid(),
                ClinicId = clinic.Id,
                Phone = Clean(request.Phone),
                Email = Clean(request.Email),
                Address = Clean(request.Address),
                Website = Clean(request.Website),
                Locale = Clean(request.Locale) ?? "en",
                DateFormat = "yyyy-MM-dd",
                TimeFormat = "HH:mm",
                DocumentationReminderHours = 12,
                WelcomeButtonText = Clean(request.WelcomeButtonText) ?? "Continue"
            };
            dbContext.ClinicSettings.Add(settings);

            var roles = SystemRoleCatalog.All.Select(definition => new Role
            {
                Id = Guid.NewGuid(),
                ClinicId = clinic.Id,
                Code = definition.Code,
                Name = definition.Name,
                IsSystem = true
            }).ToList();
            dbContext.Roles.AddRange(roles);

            foreach (var roleDefinition in SystemRoleCatalog.All)
            {
                var role = roles.Single(x => x.Code == roleDefinition.Code);
                foreach (var permissionCode in roleDefinition.Permissions)
                {
                    dbContext.RolePermissions.Add(new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        ClinicId = clinic.Id,
                        RoleId = role.Id,
                        PermissionId = permissions[permissionCode].Id
                    });
                }
            }

            foreach (var feature in features.Values)
            {
                dbContext.ClinicFeatures.Add(new ClinicFeature
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinic.Id,
                    FeatureDefinitionId = feature.Id,
                    IsEnabled = feature.IsDefaultEnabled
                });
            }

            var identityUser = new ApplicationIdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                PhoneNumber = Clean(request.Admin.Phone),
                EmailConfirmed = true,
                AccountType = AccountType.Clinic
            };

            var identityResult = await userManager.CreateAsync(identityUser, request.Admin.Password);
            if (!identityResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ClinicProvisioningResult
                {
                    Error = string.Join("; ", identityResult.Errors.Select(x => x.Description))
                };
            }

            var admin = new User
            {
                Id = Guid.NewGuid(),
                ClinicId = clinic.Id,
                IdentityUserId = identityUser.Id,
                FullName = request.Admin.FullName.Trim(),
                Email = adminEmail,
                Phone = Clean(request.Admin.Phone),
                IsClinicSuperUser = false
            };
            dbContext.Users.Add(admin);

            var adminRole = roles.Single(x => x.Code == SystemRoleCatalog.Admin);
            dbContext.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                ClinicId = clinic.Id,
                UserId = admin.Id,
                RoleId = adminRole.Id
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await auditService.WriteAsync(new AuditEvent
            {
                Scope = AuditScope.Clinic,
                ClinicId = clinic.Id,
                Action = "Clinic.ProvisioningCompleted",
                Category = "Clinic",
                EntityType = nameof(Domain.Entities.Clinic),
                EntityId = clinic.Id.ToString(),
                Description = "Clinic provisioning completed with settings, features, protected roles, role permissions and the initial Admin.",
                Metadata = new { clinic.Code, AdminUserId = admin.Id, Roles = roles.Select(x => x.Code).ToArray() }
            }, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new ClinicProvisioningResult
            {
                Clinic = ClinicService.MapDetails(clinic, settings, new InitialAdminResponse
                {
                    UserId = admin.Id,
                    FullName = admin.FullName,
                    Email = admin.Email ?? adminEmail,
                    Phone = admin.Phone,
                    Role = SystemRoleCatalog.Admin
                })
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PaginatedResponse<ClinicSummaryResponse>> SearchAsync(
        ClinicSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsPlatformActor())
            return EmptySearch(request);

        IQueryable<Domain.Entities.Clinic> query = dbContext.Clinics.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(search) || x.Code.Contains(search));
        }
        if (request.IsActive.HasValue)
            query = query.Where(x => x.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var data = await query.OrderBy(x => x.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ClinicSummaryResponse
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                IsActive = x.IsActive,
                LogoUrl = x.LogoUrl,
                TimeZoneId = x.TimeZoneId,
                PatientNumberPrefix = x.PatientNumberPrefix,
                CreatedDate = x.CreatedDate
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<ClinicSummaryResponse>
        {
            Data = data,
            Setting = new PaginationInfo { TotalCount = totalCount, RowCount = request.PageSize, CurrentPage = request.Page }
        };
    }

    public async Task<ClinicDetailsResponse?> GetByIdAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        if (!IsPlatformActor())
            return null;

        var clinic = await dbContext.Clinics.AsNoTracking().SingleOrDefaultAsync(x => x.Id == clinicId, cancellationToken);
        if (clinic is null)
            return null;
        var settings = await dbContext.ClinicSettings.AsNoTracking().SingleOrDefaultAsync(x => x.ClinicId == clinicId, cancellationToken);
        var admin = await GetInitialAdminAsync(clinicId, cancellationToken);
        return ClinicService.MapDetails(clinic, settings, admin);
    }

    public async Task<bool> UpdateAsync(Guid clinicId, UpdateClinicRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsPlatformActor())
            return false;

        var clinic = await dbContext.Clinics.SingleOrDefaultAsync(x => x.Id == clinicId, cancellationToken);
        if (clinic is null)
            return false;
        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.Clinics.AnyAsync(x => x.Id != clinicId && x.Code == code, cancellationToken))
            throw new InvalidOperationException("Clinic code already exists.");

        clinic.Name = request.Name.Trim();
        clinic.Code = code;
        clinic.LogoUrl = Clean(request.LogoUrl);
        clinic.PrimaryColor = Clean(request.PrimaryColor);
        clinic.SecondaryColor = Clean(request.SecondaryColor);
        clinic.FontFamily = Clean(request.FontFamily);
        clinic.WelcomeTitle = Clean(request.WelcomeTitle);
        clinic.WelcomeMessage = Clean(request.WelcomeMessage);
        clinic.TimeZoneId = Clean(request.TimeZoneId) ?? "UTC";
        clinic.PatientNumberPrefix = request.PatientNumberPrefix.Trim().ToUpperInvariant();
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetActiveAsync(Guid clinicId, bool isActive, CancellationToken cancellationToken = default)
    {
        if (!IsPlatformActor())
            return false;

        var clinic = await dbContext.Clinics.SingleOrDefaultAsync(x => x.Id == clinicId, cancellationToken);
        if (clinic is null)
            return false;
        if (clinic.IsActive == isActive)
            return true;

        clinic.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        await clinicAccessService.InvalidateClinicStatusAsync(clinic.Id, cancellationToken);
        await auditService.WriteAsync(new AuditEvent
        {
            Scope = AuditScope.Clinic,
            ClinicId = clinic.Id,
            Action = isActive ? "Clinic.Activated" : "Clinic.Suspended",
            Category = "Clinic",
            EntityType = nameof(Domain.Entities.Clinic),
            EntityId = clinic.Id.ToString(),
            Description = isActive ? "Clinic was activated by a Platform user." : "Clinic was suspended by a Platform user."
        }, cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<ClinicFeatureResponse>?> GetFeaturesAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        if (!IsPlatformActor() || !await dbContext.Clinics.AnyAsync(x => x.Id == clinicId, cancellationToken))
            return null;
        await catalogService.EnsureFeaturesAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapFeaturesAsync(clinicId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ClinicFeatureResponse>?> UpdateFeaturesAsync(
        Guid clinicId,
        UpdateClinicFeaturesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsPlatformActor() || !await dbContext.Clinics.AnyAsync(x => x.Id == clinicId, cancellationToken))
            return null;

        var definitions = await catalogService.EnsureFeaturesAsync(cancellationToken);
        var existing = await dbContext.ClinicFeatures
            .Where(x => x.ClinicId == clinicId)
            .ToDictionaryAsync(x => x.FeatureDefinitionId, cancellationToken);
        var affectedFeatureCodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in request.Features)
        {
            var definition = definitions.Values.Single(x => x.Code.Equals(item.Code, StringComparison.OrdinalIgnoreCase));
            if (!existing.TryGetValue(definition.Id, out var mapping))
            {
                mapping = new ClinicFeature { Id = Guid.NewGuid(), ClinicId = clinicId, FeatureDefinitionId = definition.Id };
                dbContext.ClinicFeatures.Add(mapping);
            }
            mapping.IsEnabled = item.IsEnabled;
            mapping.ConfigurationJson = Clean(item.ConfigurationJson);
            affectedFeatureCodes.Add(definition.Code);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        foreach (var featureCode in affectedFeatureCodes)
            await clinicAccessService.InvalidateFeatureAsync(clinicId, featureCode, cancellationToken);

        await auditService.WriteAsync(new AuditEvent
        {
            Scope = AuditScope.Clinic,
            ClinicId = clinicId,
            Action = "Clinic.FeaturesUpdated",
            Category = "ClinicFeature",
            EntityType = nameof(ClinicFeature),
            EntityId = clinicId.ToString(),
            Description = "Clinic feature availability was updated by a Platform user.",
            Metadata = request.Features.Select(x => new { x.Code, x.IsEnabled }).ToArray()
        }, cancellationToken);
        return await MapFeaturesAsync(clinicId, cancellationToken);
    }

    private bool IsPlatformActor() => currentActor.IsAuthenticated && currentActor.ActorType == ActorType.Platform;

    private async Task<InitialAdminResponse?> GetInitialAdminAsync(Guid clinicId, CancellationToken cancellationToken)
    {
        var adminRoleId = await dbContext.Roles.AsNoTracking()
            .Where(x => x.ClinicId == clinicId && x.Code == SystemRoleCatalog.Admin)
            .Select(x => x.Id).SingleOrDefaultAsync(cancellationToken);
        if (adminRoleId == Guid.Empty)
            return null;
        return await (from userRole in dbContext.UserRoles.AsNoTracking()
                      join user in dbContext.Users.AsNoTracking() on userRole.UserId equals user.Id
                      where userRole.ClinicId == clinicId && userRole.RoleId == adminRoleId
                      orderby user.CreatedDate
                      select new InitialAdminResponse
                      {
                          UserId = user.Id,
                          FullName = user.FullName,
                          Email = user.Email ?? string.Empty,
                          Phone = user.Phone,
                          Role = SystemRoleCatalog.Admin
                      }).FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<ClinicFeatureResponse>> MapFeaturesAsync(Guid clinicId, CancellationToken cancellationToken) =>
        await (from feature in dbContext.FeatureDefinitions.AsNoTracking()
               join mapping in dbContext.ClinicFeatures.AsNoTracking().Where(x => x.ClinicId == clinicId)
                   on feature.Id equals mapping.FeatureDefinitionId into mappings
               from mapping in mappings.DefaultIfEmpty()
               orderby feature.Name
               select new ClinicFeatureResponse
               {
                   FeatureId = feature.Id,
                   Code = feature.Code,
                   Name = feature.Name,
                   Description = feature.Description,
                   IsEnabled = mapping != null && mapping.IsEnabled,
                   ConfigurationJson = mapping == null ? null : mapping.ConfigurationJson
               }).ToListAsync(cancellationToken);

    private static PaginatedResponse<ClinicSummaryResponse> EmptySearch(ClinicSearchRequest request) => new()
    {
        Data = new List<ClinicSummaryResponse>(),
        Setting = new PaginationInfo { TotalCount = 0, RowCount = request.PageSize, CurrentPage = request.Page }
    };

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
