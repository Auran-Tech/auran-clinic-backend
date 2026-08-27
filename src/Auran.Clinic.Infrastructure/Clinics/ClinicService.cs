using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Clinics;
using Auran.Clinic.Application.Features;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Features;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Clinics;

public sealed class ClinicService(
    AuranClinicDbContext dbContext,
    ICurrentActor currentActor,
    IClinicAccessService clinicAccessService) : IClinicService
{
    public async Task<ClinicDetailsResponse?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var clinicId = await GetAccessibleClinicIdAsync(cancellationToken);
        if (!clinicId.HasValue)
            return null;

        var clinic = await dbContext.Clinics.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == clinicId.Value, cancellationToken);
        if (clinic is null)
            return null;

        var settings = await dbContext.ClinicSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ClinicId == clinic.Id, cancellationToken);
        var admin = await GetInitialAdminAsync(clinic.Id, cancellationToken);
        return MapDetails(clinic, settings, admin);
    }

    public async Task<ClinicSettingsResponse?> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var clinicId = await GetAccessibleClinicIdAsync(cancellationToken);
        if (!clinicId.HasValue)
            return null;

        var clinic = await dbContext.Clinics.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == clinicId.Value, cancellationToken);
        if (clinic is null)
            return null;

        var settings = await dbContext.ClinicSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ClinicId == clinic.Id, cancellationToken);
        return MapSettings(clinic, settings);
    }

    public async Task<ClinicSettingsResponse?> UpdateSettingsAsync(
        UpdateClinicSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var clinicId = await GetAccessibleClinicIdAsync(cancellationToken);
        if (!clinicId.HasValue)
            return null;

        var clinic = await dbContext.Clinics
            .SingleOrDefaultAsync(x => x.Id == clinicId.Value, cancellationToken);
        if (clinic is null)
            return null;

        var settings = await dbContext.ClinicSettings
            .SingleOrDefaultAsync(x => x.ClinicId == clinic.Id, cancellationToken);
        if (settings is null)
        {
            settings = new ClinicSettings { Id = Guid.NewGuid(), ClinicId = clinic.Id };
            dbContext.ClinicSettings.Add(settings);
        }

        clinic.LogoUrl = Clean(request.LogoUrl);
        clinic.PrimaryColor = Clean(request.PrimaryColor);
        clinic.SecondaryColor = Clean(request.SecondaryColor);
        clinic.FontFamily = Clean(request.FontFamily);
        clinic.WelcomeTitle = Clean(request.WelcomeTitle);
        clinic.WelcomeMessage = Clean(request.WelcomeMessage);
        clinic.TimeZoneId = Clean(request.TimeZoneId) ?? "UTC";
        clinic.PatientNumberPrefix = Clean(request.PatientNumberPrefix)?.ToUpperInvariant() ?? clinic.PatientNumberPrefix;

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
        return MapSettings(clinic, settings);
    }

    public async Task<IReadOnlyCollection<ClinicFeatureResponse>?> GetFeaturesAsync(
        CancellationToken cancellationToken = default)
    {
        var clinicId = await GetAccessibleClinicIdAsync(cancellationToken);
        if (!clinicId.HasValue)
            return null;

        return await MapFeaturesAsync(clinicId.Value, cancellationToken);
    }

    private async Task<Guid?> GetAccessibleClinicIdAsync(CancellationToken cancellationToken)
    {
        if (!currentActor.IsAuthenticated ||
            currentActor.ActorType != ActorType.Clinic ||
            !currentActor.ClinicId.HasValue)
        {
            return null;
        }

        return await clinicAccessService.IsClinicActiveAsync(currentActor.ClinicId.Value, cancellationToken)
            ? currentActor.ClinicId.Value
            : null;
    }

    private async Task<InitialAdminResponse?> GetInitialAdminAsync(Guid clinicId, CancellationToken cancellationToken)
    {
        var adminRoleId = await dbContext.Roles.AsNoTracking()
            .Where(x => x.ClinicId == clinicId && x.Code == Application.Authorization.SystemRoleCatalog.Admin)
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
                          Role = Application.Authorization.SystemRoleCatalog.Admin
                      })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<ClinicFeatureResponse>> MapFeaturesAsync(
        Guid clinicId,
        CancellationToken cancellationToken) =>
        await (from feature in dbContext.FeatureDefinitions.AsNoTracking()
               join clinicFeature in dbContext.ClinicFeatures.AsNoTracking()
                   .Where(x => x.ClinicId == clinicId)
                   on feature.Id equals clinicFeature.FeatureDefinitionId into mappings
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
               })
            .ToListAsync(cancellationToken);

    internal static ClinicDetailsResponse MapDetails(
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
        Settings = MapSettings(clinic, settings),
        InitialAdmin = admin
    };

    internal static ClinicSettingsResponse MapSettings(Domain.Entities.Clinic clinic, ClinicSettings? settings) => new()
    {
        Id = settings?.Id ?? Guid.Empty,
        ClinicId = clinic.Id,
        LogoUrl = clinic.LogoUrl,
        PrimaryColor = clinic.PrimaryColor,
        SecondaryColor = clinic.SecondaryColor,
        FontFamily = clinic.FontFamily,
        WelcomeTitle = clinic.WelcomeTitle,
        WelcomeMessage = clinic.WelcomeMessage,
        WelcomeButtonText = settings?.WelcomeButtonText,
        TimeZoneId = clinic.TimeZoneId,
        PatientNumberPrefix = clinic.PatientNumberPrefix,
        Phone = settings?.Phone,
        Email = settings?.Email,
        Address = settings?.Address,
        Website = settings?.Website,
        Locale = settings?.Locale,
        DateFormat = settings?.DateFormat,
        TimeFormat = settings?.TimeFormat,
        DocumentationReminderHours = settings?.DocumentationReminderHours ?? 12,
        PrescriptionHeader = settings?.PrescriptionHeader,
        PrescriptionFooter = settings?.PrescriptionFooter
    };

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
