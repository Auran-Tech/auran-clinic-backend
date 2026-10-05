using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Settings;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Settings;

public sealed class ClinicSettingsService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IClinicSettingsService
{
    public async Task<ClinicSettingsResponse?> GetAsync(CancellationToken cancellationToken = default)
    {
        var clinicId = currentUserContext.ClinicId;
        if (!clinicId.HasValue)
            return null;

        var clinic = await dbContext.Clinics
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == clinicId.Value, cancellationToken);
        if (clinic is null)
            return null;

        var settings = await dbContext.ClinicSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ClinicId == clinicId.Value, cancellationToken);

        return Map(clinic, settings);
    }

    public async Task<ClinicSettingsResponse?> UpdateAsync(
        UpdateClinicSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var clinicId = currentUserContext.ClinicId;
        if (!clinicId.HasValue)
            return null;

        var clinic = await dbContext.Clinics
            .SingleOrDefaultAsync(item => item.Id == clinicId.Value, cancellationToken);
        if (clinic is null)
            return null;

        var settings = await dbContext.ClinicSettings
            .SingleOrDefaultAsync(item => item.ClinicId == clinicId.Value, cancellationToken);

        var now = DateTime.UtcNow;
        if (settings is null)
        {
            settings = new ClinicSettings
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId.Value,
                CreatedDate = now
            };
            dbContext.ClinicSettings.Add(settings);
        }

        clinic.Name = request.ClinicName.Trim();
        clinic.LogoUrl = Clean(request.LogoUrl);
        clinic.PrimaryColor = Clean(request.PrimaryColor);
        clinic.SecondaryColor = Clean(request.SecondaryColor);
        clinic.FontFamily = Clean(request.FontFamily);
        clinic.WelcomeTitle = Clean(request.WelcomeTitle);
        clinic.WelcomeMessage = Clean(request.WelcomeMessage);
        clinic.TimeZoneId = Clean(request.TimeZoneId);
        clinic.PatientNumberPrefix = NormalizeCode(request.PatientNumberPrefix);
        clinic.UpdatedDate = now;

        settings.Phone = Clean(request.Phone);
        settings.Email = NormalizeEmail(request.Email);
        settings.Address = Clean(request.Address);
        settings.Website = Clean(request.Website);
        settings.Locale = Clean(request.Locale) ?? "en";
        settings.DateFormat = Clean(request.DateFormat) ?? "yyyy-MM-dd";
        settings.TimeFormat = Clean(request.TimeFormat) ?? "HH:mm";
        settings.DocumentationReminderHours = request.DocumentationReminderHours;
        settings.PrescriptionHeader = Clean(request.PrescriptionHeader);
        settings.PrescriptionFooter = Clean(request.PrescriptionFooter);
        settings.WelcomeButtonText = Clean(request.WelcomeButtonText);
        settings.UpdatedDate = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "Settings.ClinicUpdated",
            nameof(ClinicSettings),
            settings.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["clinicId"] = clinicId.Value,
                ["locale"] = settings.Locale,
                ["timeZoneId"] = clinic.TimeZoneId
            },
            cancellationToken);

        return Map(clinic, settings);
    }

    private static ClinicSettingsResponse Map(Clinic clinic, ClinicSettings? settings) =>
        new(
            clinic.Name,
            clinic.Code,
            clinic.LogoUrl,
            clinic.PrimaryColor,
            clinic.SecondaryColor,
            clinic.FontFamily,
            clinic.WelcomeTitle,
            clinic.WelcomeMessage,
            clinic.TimeZoneId,
            clinic.PatientNumberPrefix,
            settings?.Phone,
            settings?.Email,
            settings?.Address,
            settings?.Website,
            settings?.Locale,
            settings?.DateFormat,
            settings?.TimeFormat,
            settings?.DocumentationReminderHours ?? 12,
            settings?.PrescriptionHeader,
            settings?.PrescriptionFooter,
            settings?.WelcomeButtonText);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeEmail(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static string? NormalizeCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}
