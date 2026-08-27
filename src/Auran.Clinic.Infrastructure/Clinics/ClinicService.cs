using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Clinics;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Clinics;

public sealed class ClinicService(
    AuranClinicDbContext dbContext,
    UserManager<ApplicationIdentityUser> userManager,
    ICurrentUser currentUser,
    IAuditService auditService) : IClinicService
{
    public async Task<ClinicProvisioningResult> CreateAsync(
        CreateClinicRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsSuperUser)
            return new ClinicProvisioningResult { Error = "Only a Super User can provision a clinic." };

        var clinicCode = request.Code.Trim().ToUpperInvariant();
        var adminEmail = request.Admin.Email.Trim().ToLowerInvariant();

        if (await dbContext.Clinics.AnyAsync(x => x.Code == clinicCode, cancellationToken))
            return new ClinicProvisioningResult { Error = "Clinic code already exists.", IsConflict = true };

        if (await userManager.FindByEmailAsync(adminEmail) is not null)
            return new ClinicProvisioningResult { Error = "Admin email is already in use.", IsConflict = true };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
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

            var permissions = await EnsurePermissionCatalogAsync(cancellationToken);
            var roles = CreateSystemRoles(clinic.Id);
            dbContext.Roles.AddRange(roles);

            foreach (var roleDefinition in SystemRoleCatalog.All)
            {
                var role = roles.Single(x => x.Code == roleDefinition.Code);
                foreach (var permissionCode in roleDefinition.Permissions)
                {
                    var permission = permissions[permissionCode];
                    dbContext.RolePermissions.Add(new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        ClinicId = clinic.Id,
                        RoleId = role.Id,
                        PermissionId = permission.Id
                    });
                }
            }

            var identityUser = new ApplicationIdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                PhoneNumber = Clean(request.Admin.Phone),
                EmailConfirmed = true
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
                IsSuperUser = false
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
                ClinicId = clinic.Id,
                Action = "Clinic.ProvisioningCompleted",
                Category = "Clinic",
                EntityType = nameof(Domain.Entities.Clinic),
                EntityId = clinic.Id.ToString(),
                Description = "Clinic provisioning completed with default settings, system roles, permissions and initial admin.",
                Metadata = new
                {
                    clinic.Code,
                    AdminUserId = admin.Id,
                    AdminEmail = admin.Email,
                    Roles = roles.Select(x => x.Code).ToArray()
                }
            }, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new ClinicProvisioningResult
            {
                Clinic = MapDetails(clinic, settings, new InitialAdminResponse
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
        IQueryable<Domain.Entities.Clinic> query = dbContext.Clinics.AsNoTracking();

        if (!currentUser.IsSuperUser)
        {
            if (!currentUser.ClinicId.HasValue)
                return EmptySearch(request);

            query = query.Where(x => x.Id == currentUser.ClinicId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(search) || x.Code.Contains(search));
        }

        if (request.IsActive.HasValue)
            query = query.Where(x => x.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var clinics = await query
            .OrderBy(x => x.Name)
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
            Data = clinics,
            Setting = new PaginationInfo
            {
                TotalCount = totalCount,
                RowCount = request.PageSize,
                CurrentPage = request.Page
            }
        };
    }

    public async Task<ClinicDetailsResponse?> GetByIdAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        if (!CanAccessClinic(clinicId))
            return null;

        var clinic = await dbContext.Clinics.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == clinicId, cancellationToken);
        if (clinic is null)
            return null;

        var settings = await dbContext.ClinicSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ClinicId == clinicId, cancellationToken);

        var admin = await GetInitialAdminAsync(clinicId, cancellationToken);
        return MapDetails(clinic, settings, admin);
    }

    public async Task<bool> UpdateAsync(
        Guid clinicId,
        UpdateClinicRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!CanAccessClinic(clinicId))
            return false;

        var clinic = await dbContext.Clinics.SingleOrDefaultAsync(x => x.Id == clinicId, cancellationToken);
        if (clinic is null)
            return false;

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.Clinics.AnyAsync(x => x.Id != clinicId && x.Code == normalizedCode, cancellationToken))
            throw new InvalidOperationException("Clinic code already exists.");

        clinic.Name = request.Name.Trim();
        clinic.Code = normalizedCode;
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
        if (!CanAccessClinic(clinicId))
            return false;

        var clinic = await dbContext.Clinics.SingleOrDefaultAsync(x => x.Id == clinicId, cancellationToken);
        if (clinic is null)
            return false;

        if (clinic.IsActive == isActive)
            return true;

        clinic.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(new AuditEvent
        {
            ClinicId = clinic.Id,
            Action = isActive ? "Clinic.Activated" : "Clinic.Deactivated",
            Category = "Clinic",
            EntityType = nameof(Domain.Entities.Clinic),
            EntityId = clinic.Id.ToString(),
            Description = isActive ? "Clinic was activated." : "Clinic was deactivated."
        }, cancellationToken);

        return true;
    }

    public async Task<ClinicSettingsResponse?> GetSettingsAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        if (!CanAccessClinic(clinicId))
            return null;

        var settings = await dbContext.ClinicSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ClinicId == clinicId, cancellationToken);
        return settings is null ? null : MapSettings(settings);
    }

    public async Task<ClinicSettingsResponse?> UpdateSettingsAsync(
        Guid clinicId,
        UpdateClinicSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!CanAccessClinic(clinicId))
            return null;

        var clinicExists = await dbContext.Clinics.AnyAsync(x => x.Id == clinicId, cancellationToken);
        if (!clinicExists)
            return null;

        var settings = await dbContext.ClinicSettings.SingleOrDefaultAsync(x => x.ClinicId == clinicId, cancellationToken);
        if (settings is null)
        {
            settings = new ClinicSettings
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId
            };
            dbContext.ClinicSettings.Add(settings);
        }

        settings.Phone = Clean(request.Phone);
        settings.Email = Clean(request.Email);
        settings.Address = Clean(request.Address);
        settings.Website = Clean(request.Website);
        settings.Locale = Clean(request.Locale) ?? "en";
        settings.DateFormat = Clean(request.DateFormat) ?? "yyyy-MM-dd";
        settings.TimeFormat = Clean(request.TimeFormat) ?? "HH:mm";
        settings.DocumentationReminderHours = request.DocumentationReminderHours;
        settings.PrescriptionHeader = Clean(request.PrescriptionHeader);
        settings.PrescriptionFooter = Clean(request.PrescriptionFooter);
        settings.WelcomeButtonText = Clean(request.WelcomeButtonText) ?? "Continue";

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapSettings(settings);
    }

    private async Task<Dictionary<string, Permission>> EnsurePermissionCatalogAsync(CancellationToken cancellationToken)
    {
        var codes = SystemPermissionCatalog.All.Select(x => x.Code).ToArray();
        var existing = await dbContext.Permissions
            .Where(x => codes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        foreach (var definition in SystemPermissionCatalog.All)
        {
            if (existing.ContainsKey(definition.Code))
                continue;

            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Code = definition.Code,
                Name = definition.Name,
                Group = definition.Group
            };
            dbContext.Permissions.Add(permission);
            existing[definition.Code] = permission;
        }

        return existing;
    }

    private static List<Role> CreateSystemRoles(Guid clinicId) =>
        SystemRoleCatalog.All.Select(definition => new Role
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            Code = definition.Code,
            Name = definition.Name,
            IsSystem = true
        }).ToList();

    private async Task<InitialAdminResponse?> GetInitialAdminAsync(Guid clinicId, CancellationToken cancellationToken)
    {
        var adminRoleId = await dbContext.Roles.AsNoTracking()
            .Where(x => x.ClinicId == clinicId && x.Code == SystemRoleCatalog.Admin)
            .Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);
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
                      })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private bool CanAccessClinic(Guid clinicId) =>
        currentUser.IsSuperUser || currentUser.ClinicId == clinicId;

    private static ClinicDetailsResponse MapDetails(
        Domain.Entities.Clinic clinic,
        ClinicSettings? settings,
        InitialAdminResponse? admin) => new()
    {
        Id = clinic.Id,
        Name = clinic.Name,
        Code = clinic.Code,
        IsActive = clinic.IsActive,
        LogoUrl = clinic.LogoUrl,
        PrimaryColor = clinic.PrimaryColor,
        SecondaryColor = clinic.SecondaryColor,
        FontFamily = clinic.FontFamily,
        WelcomeTitle = clinic.WelcomeTitle,
        WelcomeMessage = clinic.WelcomeMessage,
        TimeZoneId = clinic.TimeZoneId,
        PatientNumberPrefix = clinic.PatientNumberPrefix,
        CreatedDate = clinic.CreatedDate,
        UpdatedDate = clinic.UpdatedDate,
        Settings = settings is null ? null : MapSettings(settings),
        InitialAdmin = admin
    };

    private static ClinicSettingsResponse MapSettings(ClinicSettings settings) => new()
    {
        Id = settings.Id,
        ClinicId = settings.ClinicId,
        Phone = settings.Phone,
        Email = settings.Email,
        Address = settings.Address,
        Website = settings.Website,
        Locale = settings.Locale,
        DateFormat = settings.DateFormat,
        TimeFormat = settings.TimeFormat,
        DocumentationReminderHours = settings.DocumentationReminderHours,
        PrescriptionHeader = settings.PrescriptionHeader,
        PrescriptionFooter = settings.PrescriptionFooter,
        WelcomeButtonText = settings.WelcomeButtonText
    };

    private static PaginatedResponse<ClinicSummaryResponse> EmptySearch(ClinicSearchRequest request) => new()
    {
        Data = new List<ClinicSummaryResponse>(),
        Setting = new PaginationInfo
        {
            TotalCount = 0,
            RowCount = request.PageSize,
            CurrentPage = request.Page
        }
    };

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
