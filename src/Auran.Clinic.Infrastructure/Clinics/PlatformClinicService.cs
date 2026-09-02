using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Clinics;
using Auran.Clinic.Application.Codes;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Clinics;

public sealed class PlatformClinicService(
    AuranClinicDbContext dbContext,
    UserManager<ApplicationIdentityUser> userManager,
    ICodeGeneratorService codeGeneratorService,
    ClinicScopeOverride clinicScopeOverride,
    IHttpContextAccessor httpContextAccessor,
    ILogger<PlatformClinicService> logger) : IPlatformClinicService
{
    public async Task<ClinicProvisioningResult> CreateAsync(
        CreateClinicRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePlatformActor();

        var validationError = ValidateCreateRequest(request);
        if (validationError is not null)
            return new ClinicProvisioningResult { Error = validationError };

        var adminEmail = request.Admin.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(adminEmail) is not null)
        {
            return new ClinicProvisioningResult
            {
                Error = "The initial admin email is already in use.",
                IsConflict = true
            };
        }

        var adminRole = await dbContext.Roles.AsNoTracking()
            .SingleOrDefaultAsync(role => role.Code == SystemRoleCatalog.Admin, cancellationToken);
        if (adminRole is null)
            return new ClinicProvisioningResult { Error = "The system role catalog is not initialized." };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var clinicCode = await codeGeneratorService.GenerateAsync(
                CodeScope.Platform,
                clinicId: null,
                CodeType.Clinic,
                request.CodePrefix,
                cancellationToken);

            var clinic = new Domain.Entities.Clinic
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Code = clinicCode,
                IsActive = true,
                TimeZoneId = Clean(request.TimeZoneId),
                PatientNumberPrefix = NormalizeOptionalCode(request.PatientNumberPrefix),
                CreatedDate = now
            };
            dbContext.Clinics.Add(clinic);

            dbContext.ClinicSettings.Add(new ClinicSettings
            {
                Id = Guid.NewGuid(),
                ClinicId = clinic.Id,
                Phone = Clean(request.Phone),
                Email = NormalizeEmail(request.Email),
                Address = Clean(request.Address),
                Website = Clean(request.Website),
                Locale = Clean(request.Locale) ?? "en",
                DateFormat = "yyyy-MM-dd",
                TimeFormat = "HH:mm",
                DocumentationReminderHours = 12,
                CreatedDate = now
            });

            using var clinicScope = clinicScopeOverride.Enter(clinic.Id);

            var identityUser = new ApplicationIdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                PhoneNumber = Clean(request.Admin.Phone),
                EmailConfirmed = true,
                LockoutEnabled = true,
                AccountType = AccountType.Clinic
            };

            var identityResult = await userManager.CreateAsync(identityUser, request.Admin.Password);
            if (!identityResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ClinicProvisioningResult
                {
                    Error = string.Join("; ", identityResult.Errors.Select(error => error.Description))
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
                IsSuperUser = true,
                IsActive = true,
                CreatedDate = now
            };
            dbContext.Users.Add(admin);

            dbContext.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                ClinicId = clinic.Id,
                UserId = admin.Id,
                RoleId = adminRole.Id,
                CreatedDate = now
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Platform actor {PlatformUserId} provisioned clinic {ClinicId} ({ClinicCode}) with initial Super User {UserId}.",
                PlatformUserId,
                clinic.Id,
                clinic.Code,
                admin.Id);

            return new ClinicProvisioningResult
            {
                Clinic = await MapDetailsAsync(clinic, admin.Id, adminEmail, cancellationToken)
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<ClinicSummaryResponse>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        EnsurePlatformActor();

        return await dbContext.Clinics.AsNoTracking()
            .OrderBy(clinic => clinic.Name)
            .Select(clinic => new ClinicSummaryResponse(
                clinic.Id,
                clinic.Name,
                clinic.Code,
                clinic.IsActive,
                clinic.TimeZoneId,
                clinic.PatientNumberPrefix,
                clinic.CreatedDate))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClinicDetailsResponse?> GetAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        EnsurePlatformActor();

        var clinic = await dbContext.Clinics.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == clinicId, cancellationToken);
        return clinic is null ? null : await MapDetailsAsync(clinic, null, null, cancellationToken);
    }

    public async Task<ClinicDetailsResponse?> UpdateAsync(
        Guid clinicId,
        UpdateClinicRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePlatformActor();

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Clinic name is required.", nameof(request));

        var clinic = await dbContext.Clinics
            .SingleOrDefaultAsync(item => item.Id == clinicId, cancellationToken);
        if (clinic is null)
            return null;

        var settings = await dbContext.ClinicSettings
            .SingleOrDefaultAsync(item => item.ClinicId == clinicId, cancellationToken);
        if (settings is null)
        {
            settings = new ClinicSettings
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                CreatedDate = DateTime.UtcNow
            };
            dbContext.ClinicSettings.Add(settings);
        }

        clinic.Name = request.Name.Trim();
        clinic.TimeZoneId = Clean(request.TimeZoneId);
        clinic.PatientNumberPrefix = NormalizeOptionalCode(request.PatientNumberPrefix);
        clinic.UpdatedDate = DateTime.UtcNow;

        settings.Phone = Clean(request.Phone);
        settings.Email = NormalizeEmail(request.Email);
        settings.Address = Clean(request.Address);
        settings.Website = Clean(request.Website);
        settings.Locale = Clean(request.Locale) ?? settings.Locale ?? "en";
        settings.UpdatedDate = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Platform actor {PlatformUserId} updated clinic {ClinicId}.",
            PlatformUserId,
            clinicId);

        return await MapDetailsAsync(clinic, null, null, cancellationToken);
    }

    public async Task<bool> SetActiveAsync(
        Guid clinicId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        EnsurePlatformActor();

        var clinic = await dbContext.Clinics
            .SingleOrDefaultAsync(item => item.Id == clinicId, cancellationToken);
        if (clinic is null)
            return false;

        clinic.IsActive = isActive;
        clinic.UpdatedDate = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Platform actor {PlatformUserId} set clinic {ClinicId} active state to {IsActive}.",
            PlatformUserId,
            clinicId,
            isActive);

        return true;
    }

    private async Task<ClinicDetailsResponse> MapDetailsAsync(
        Domain.Entities.Clinic clinic,
        Guid? knownAdminUserId,
        string? knownAdminEmail,
        CancellationToken cancellationToken)
    {
        var settings = await dbContext.ClinicSettings.AsNoTracking()
            .SingleOrDefaultAsync(item => item.ClinicId == clinic.Id, cancellationToken);

        Guid? adminUserId = knownAdminUserId;
        string? adminEmail = knownAdminEmail;

        if (!adminUserId.HasValue)
        {
            var adminRoleId = await dbContext.Roles.AsNoTracking()
                .Where(role => role.Code == SystemRoleCatalog.Admin)
                .Select(role => role.Id)
                .SingleOrDefaultAsync(cancellationToken);

            if (adminRoleId != Guid.Empty)
            {
                using var clinicScope = clinicScopeOverride.Enter(clinic.Id);
                var admin = await (
                        from userRole in dbContext.UserRoles.AsNoTracking()
                        join user in dbContext.Users.AsNoTracking()
                            on userRole.UserId equals user.Id
                        where userRole.RoleId == adminRoleId && user.IsActive
                        orderby user.CreatedDate
                        select new { user.Id, user.Email })
                    .FirstOrDefaultAsync(cancellationToken);

                adminUserId = admin?.Id;
                adminEmail = admin?.Email;
            }
        }

        return new ClinicDetailsResponse
        {
            Id = clinic.Id,
            Name = clinic.Name,
            Code = clinic.Code,
            IsActive = clinic.IsActive,
            TimeZoneId = clinic.TimeZoneId,
            PatientNumberPrefix = clinic.PatientNumberPrefix,
            Phone = settings?.Phone,
            Email = settings?.Email,
            Address = settings?.Address,
            Website = settings?.Website,
            Locale = settings?.Locale,
            InitialAdminUserId = adminUserId,
            InitialAdminEmail = adminEmail
        };
    }

    private void EnsurePlatformActor()
    {
        var actorType = httpContextAccessor.HttpContext?.User.FindFirst("actor_type")?.Value;
        if (!string.Equals(actorType, ActorType.Platform.ToString(), StringComparison.Ordinal))
            throw new UnauthorizedAccessException("A Platform actor is required.");
    }

    private Guid? PlatformUserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirst("platform_user_id")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    private static string? ValidateCreateRequest(CreateClinicRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return "Clinic name is required.";
        if (string.IsNullOrWhiteSpace(request.CodePrefix))
            return "Clinic code prefix is required.";
        if (request.Admin is null || string.IsNullOrWhiteSpace(request.Admin.FullName))
            return "Initial admin full name is required.";
        if (string.IsNullOrWhiteSpace(request.Admin.Email))
            return "Initial admin email is required.";
        if (string.IsNullOrWhiteSpace(request.Admin.Password))
            return "Initial admin password is required.";
        return null;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeEmail(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static string? NormalizeOptionalCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}
