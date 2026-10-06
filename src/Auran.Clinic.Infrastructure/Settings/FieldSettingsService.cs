using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Settings;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Settings;

public sealed class FieldSettingsService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IFieldSettingsService
{
    public async Task<FieldSettingsResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var sections = await dbContext.PatientProfileSections
            .AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

        var fields = await dbContext.PatientProfileFields
            .AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Label)
            .ToListAsync(cancellationToken);

        var fieldIds = fields.Select(item => item.Id).ToArray();
        var profileOptions = await dbContext.PatientProfileFieldOptions
            .AsNoTracking()
            .Where(item => fieldIds.Contains(item.FieldId))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Label)
            .ToListAsync(cancellationToken);

        var clinicalFields = await dbContext.ClinicalFields
            .AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

        var clinicalFieldIds = clinicalFields.Select(item => item.Id).ToArray();
        var clinicalOptions = await dbContext.ClinicalFieldOptions
            .AsNoTracking()
            .Where(item => clinicalFieldIds.Contains(item.ClinicalFieldId))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Label)
            .ToListAsync(cancellationToken);

        return new FieldSettingsResponse(
            sections.Select(section => new PatientProfileSectionSettingsResponse(
                section.Id,
                section.Name,
                section.SortOrder,
                section.IsSystem,
                section.IsEnabled,
                fields
                    .Where(field => field.SectionId == section.Id)
                    .Select(field => new PatientProfileFieldSettingsResponse(
                        field.Id,
                        field.Label,
                        field.FieldType,
                        field.IsRequired,
                        field.IsEnabled,
                        field.SortOrder,
                        profileOptions
                            .Where(option => option.FieldId == field.Id)
                            .Select(option => new FieldOptionSettingsResponse(
                                option.Id,
                                option.Label,
                                option.Value,
                                option.SortOrder))
                            .ToArray()))
                    .ToArray()))
                .ToArray(),
            clinicalFields.Select(field => new ClinicalFieldSettingsResponse(
                field.Id,
                field.Name,
                field.FieldType,
                field.Unit,
                field.IsEnabled,
                field.SortOrder,
                clinicalOptions
                    .Where(option => option.ClinicalFieldId == field.Id)
                    .Select(option => new FieldOptionSettingsResponse(
                        option.Id,
                        option.Label,
                        option.Value,
                        option.SortOrder))
                    .ToArray()))
                .ToArray());
    }

    public async Task<FieldSettingsResponse?> SaveAsync(
        SaveFieldSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.UserId.HasValue || !currentUserContext.ClinicId.HasValue)
            return null;

        var userId = currentUserContext.UserId.Value;
        var clinicId = currentUserContext.ClinicId.Value;
        var now = DateTime.UtcNow;

        var existingSections = await dbContext.PatientProfileSections
            .ToListAsync(cancellationToken);
        var existingFields = await dbContext.PatientProfileFields
            .ToListAsync(cancellationToken);
        var existingProfileOptions = await dbContext.PatientProfileFieldOptions
            .ToListAsync(cancellationToken);
        var existingClinicalFields = await dbContext.ClinicalFields
            .ToListAsync(cancellationToken);
        var existingClinicalOptions = await dbContext.ClinicalFieldOptions
            .ToListAsync(cancellationToken);

        var requestedSectionIds = request.ProfileSections
            .Where(item => item.Id.HasValue)
            .Select(item => item.Id!.Value)
            .ToHashSet();

        var requestedFieldIds = request.ProfileSections
            .SelectMany(item => item.Fields)
            .Where(item => item.Id.HasValue)
            .Select(item => item.Id!.Value)
            .ToHashSet();

        var requestedClinicalFieldIds = request.ClinicalFields
            .Where(item => item.Id.HasValue)
            .Select(item => item.Id!.Value)
            .ToHashSet();

        if (requestedSectionIds.Any(id => existingSections.All(item => item.Id != id)) ||
            requestedFieldIds.Any(id => existingFields.All(item => item.Id != id)) ||
            requestedClinicalFieldIds.Any(id => existingClinicalFields.All(item => item.Id != id)))
        {
            return null;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var section in existingSections.Where(item => !requestedSectionIds.Contains(item.Id)))
            {
                if (section.IsSystem)
                    continue;

                section.IsEnabled = false;
                section.UpdatedDate = now;
                section.UpdatedByUserId = userId;
            }

            foreach (var field in existingFields.Where(item => !requestedFieldIds.Contains(item.Id)))
            {
                field.IsEnabled = false;
                field.UpdatedDate = now;
                field.UpdatedByUserId = userId;
            }

            foreach (var requestedSection in request.ProfileSections)
            {
                PatientProfileSection section;
                if (requestedSection.Id.HasValue)
                {
                    section = existingSections.Single(item => item.Id == requestedSection.Id.Value);
                    section.Name = requestedSection.Name.Trim();
                    section.SortOrder = requestedSection.SortOrder;
                    section.IsEnabled = section.IsSystem || requestedSection.IsEnabled;
                    section.UpdatedDate = now;
                    section.UpdatedByUserId = userId;
                }
                else
                {
                    section = new PatientProfileSection
                    {
                        Id = Guid.NewGuid(),
                        ClinicId = clinicId,
                        Name = requestedSection.Name.Trim(),
                        SortOrder = requestedSection.SortOrder,
                        IsSystem = false,
                        IsEnabled = requestedSection.IsEnabled,
                        CreatedDate = now,
                        CreateByUserId = userId
                    };
                    dbContext.PatientProfileSections.Add(section);
                    existingSections.Add(section);
                }

                foreach (var requestedField in requestedSection.Fields)
                {
                    PatientProfileField field;
                    if (requestedField.Id.HasValue)
                    {
                        field = existingFields.Single(item => item.Id == requestedField.Id.Value);
                        field.SectionId = section.Id;
                        field.Label = requestedField.Label.Trim();
                        field.FieldType = requestedField.FieldType;
                        field.IsRequired = requestedField.IsRequired;
                        field.IsEnabled = requestedField.IsEnabled;
                        field.SortOrder = requestedField.SortOrder;
                        field.UpdatedDate = now;
                        field.UpdatedByUserId = userId;
                    }
                    else
                    {
                        field = new PatientProfileField
                        {
                            Id = Guid.NewGuid(),
                            ClinicId = clinicId,
                            SectionId = section.Id,
                            Label = requestedField.Label.Trim(),
                            FieldType = requestedField.FieldType,
                            IsRequired = requestedField.IsRequired,
                            IsEnabled = requestedField.IsEnabled,
                            SortOrder = requestedField.SortOrder,
                            CreatedDate = now,
                            CreateByUserId = userId
                        };
                        dbContext.PatientProfileFields.Add(field);
                        existingFields.Add(field);
                    }

                    var oldOptions = existingProfileOptions
                        .Where(option => option.FieldId == field.Id)
                        .ToList();
                    dbContext.PatientProfileFieldOptions.RemoveRange(oldOptions);

                    foreach (var option in requestedField.Options)
                    {
                        dbContext.PatientProfileFieldOptions.Add(new PatientProfileFieldOption
                        {
                            Id = Guid.NewGuid(),
                            ClinicId = clinicId,
                            FieldId = field.Id,
                            Label = option.Label.Trim(),
                            Value = option.Value.Trim(),
                            SortOrder = option.SortOrder,
                            CreatedDate = now,
                            CreateByUserId = userId
                        });
                    }
                }
            }

            foreach (var field in existingClinicalFields.Where(item => !requestedClinicalFieldIds.Contains(item.Id)))
            {
                field.IsEnabled = false;
                field.UpdatedDate = now;
                field.UpdatedByUserId = userId;
            }

            foreach (var requestedField in request.ClinicalFields)
            {
                ClinicalField field;
                if (requestedField.Id.HasValue)
                {
                    field = existingClinicalFields.Single(item => item.Id == requestedField.Id.Value);
                    field.Name = requestedField.Name.Trim();
                    field.FieldType = requestedField.FieldType;
                    field.Unit = Clean(requestedField.Unit);
                    field.IsEnabled = requestedField.IsEnabled;
                    field.SortOrder = requestedField.SortOrder;
                    field.UpdatedDate = now;
                    field.UpdatedByUserId = userId;
                }
                else
                {
                    field = new ClinicalField
                    {
                        Id = Guid.NewGuid(),
                        ClinicId = clinicId,
                        Name = requestedField.Name.Trim(),
                        FieldType = requestedField.FieldType,
                        Unit = Clean(requestedField.Unit),
                        IsEnabled = requestedField.IsEnabled,
                        SortOrder = requestedField.SortOrder,
                        CreatedDate = now,
                        CreateByUserId = userId
                    };
                    dbContext.ClinicalFields.Add(field);
                    existingClinicalFields.Add(field);
                }

                var oldOptions = existingClinicalOptions
                    .Where(option => option.ClinicalFieldId == field.Id)
                    .ToList();
                dbContext.ClinicalFieldOptions.RemoveRange(oldOptions);

                foreach (var option in requestedField.Options)
                {
                    dbContext.ClinicalFieldOptions.Add(new ClinicalFieldOption
                    {
                        Id = Guid.NewGuid(),
                        ClinicId = clinicId,
                        ClinicalFieldId = field.Id,
                        Label = option.Label.Trim(),
                        Value = option.Value.Trim(),
                        SortOrder = option.SortOrder,
                        CreatedDate = now,
                        CreateByUserId = userId
                    });
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            await auditService.WriteAsync(
                "Settings.DynamicFieldsUpdated",
                nameof(PatientProfileField),
                null,
                new Dictionary<string, object?>
                {
                    ["profileSectionCount"] = request.ProfileSections.Count,
                    ["profileFieldCount"] = request.ProfileSections.Sum(item => item.Fields.Count),
                    ["clinicalFieldCount"] = request.ClinicalFields.Count
                },
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return await GetAsync(cancellationToken);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
