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
    public async Task<ClinicSettingsResponse?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        if (!TryGetClinicId(out var clinicId))
            return null;

        var clinic = await dbContext.Clinics.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == clinicId, cancellationToken);
        if (clinic is null)
            return null;

        var settings = await dbContext.ClinicSettings.AsNoTracking()
            .SingleOrDefaultAsync(item => item.ClinicId == clinicId, cancellationToken);

        return Map(clinic, settings);
    }

    public async Task<ClinicSettingsResult> UpdateAsync(
        UpdateClinicSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.IsAuthenticated ||
            currentUserContext.UserId is not Guid userId ||
            currentUserContext.ClinicId is not Guid clinicId)
        {
            return new ClinicSettingsResult(ClinicSettingsOutcome.Unauthenticated);
        }

        var clinic = await dbContext.Clinics
            .SingleOrDefaultAsync(item => item.Id == clinicId, cancellationToken);
        if (clinic is null)
            return new ClinicSettingsResult(ClinicSettingsOutcome.NotFound);

        var settings = await dbContext.ClinicSettings
            .SingleOrDefaultAsync(item => item.ClinicId == clinicId, cancellationToken);

        var now = DateTime.UtcNow;

        clinic.Name = request.ClinicName.Trim();
        clinic.LogoUrl = Clean(request.LogoUrl);
        clinic.PrimaryColor = Clean(request.PrimaryColor);
        clinic.SecondaryColor = Clean(request.SecondaryColor);
        clinic.FontFamily = Clean(request.FontFamily);
        clinic.WelcomeTitle = Clean(request.WelcomeTitle);
        clinic.WelcomeMessage = Clean(request.WelcomeMessage);
        clinic.TimeZoneId = Clean(request.TimeZoneId);
        clinic.PatientNumberPrefix = Clean(request.PatientNumberPrefix)?.ToUpperInvariant();
        clinic.UpdatedDate = now;
        clinic.UpdatedByUserId = userId;

        if (settings is null)
        {
            settings = new ClinicSettings
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                CreatedDate = now,
                CreateByUserId = userId
            };
            dbContext.ClinicSettings.Add(settings);
        }
        else
        {
            settings.UpdatedDate = now;
            settings.UpdatedByUserId = userId;
        }

        settings.Phone = Clean(request.Phone);
        settings.Email = Clean(request.Email)?.ToLowerInvariant();
        settings.Address = Clean(request.Address);
        settings.Website = Clean(request.Website);
        settings.Locale = Clean(request.Locale);
        settings.DateFormat = Clean(request.DateFormat);
        settings.TimeFormat = Clean(request.TimeFormat);
        settings.DocumentationReminderHours = request.DocumentationReminderHours;
        settings.PrescriptionHeader = Clean(request.PrescriptionHeader);
        settings.PrescriptionFooter = Clean(request.PrescriptionFooter);
        settings.WelcomeButtonText = Clean(request.WelcomeButtonText);

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "ClinicSettings.Updated",
            nameof(ClinicSettings),
            settings.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["clinicId"] = clinicId,
                ["timeZoneId"] = clinic.TimeZoneId,
                ["locale"] = settings.Locale,
                ["patientNumberPrefix"] = clinic.PatientNumberPrefix
            },
            cancellationToken);

        return new ClinicSettingsResult(
            ClinicSettingsOutcome.Success,
            Map(clinic, settings));
    }

    private bool TryGetClinicId(out Guid clinicId)
    {
        if (currentUserContext.IsAuthenticated &&
            currentUserContext.ClinicId is Guid currentClinicId)
        {
            clinicId = currentClinicId;
            return true;
        }

        clinicId = Guid.Empty;
        return false;
    }

    private static ClinicSettingsResponse Map(
        Domain.Entities.Clinic clinic,
        ClinicSettings? settings) =>
        new(
            clinic.Id,
            clinic.Name,
            clinic.Code,
            clinic.IsActive,
            clinic.LogoUrl,
            clinic.PrimaryColor,
            clinic.SecondaryColor,
            clinic.FontFamily,
            clinic.WelcomeTitle,
            clinic.WelcomeMessage,
            settings?.WelcomeButtonText,
            clinic.TimeZoneId,
            settings?.Locale,
            settings?.DateFormat,
            settings?.TimeFormat,
            clinic.PatientNumberPrefix,
            settings?.Phone,
            settings?.Email,
            settings?.Address,
            settings?.Website,
            settings?.DocumentationReminderHours ?? 12,
            settings?.PrescriptionHeader,
            settings?.PrescriptionFooter);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
