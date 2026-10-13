using System.Text.Json;
using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.PatientProfiles;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.PatientProfiles;

public sealed class PatientProfileService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IPatientProfileService
{
    private static readonly DynamicFieldType[] EditableFieldTypes =
    [
        DynamicFieldType.Text,
        DynamicFieldType.LongText,
        DynamicFieldType.Number,
        DynamicFieldType.Boolean,
        DynamicFieldType.Date,
        DynamicFieldType.SingleSelect,
        DynamicFieldType.MultiSelect
    ];

    public async Task<PatientProfileConfigurationResponse> GetConfigurationAsync(
        CancellationToken cancellationToken = default)
    {
        var sections = await dbContext.PatientProfileSections.AsNoTracking()
            .Where(section => section.IsEnabled)
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Name)
            .ToListAsync(cancellationToken);

        var sectionIds = sections.Select(section => section.Id).ToArray();
        var fields = sectionIds.Length == 0
            ? new List<PatientProfileField>()
            : await dbContext.PatientProfileFields.AsNoTracking()
                .Where(field => field.IsEnabled && sectionIds.Contains(field.SectionId))
                .OrderBy(field => field.SortOrder)
                .ThenBy(field => field.Label)
                .ToListAsync(cancellationToken);

        var fieldIds = fields.Select(field => field.Id).ToArray();
        var options = fieldIds.Length == 0
            ? new List<PatientProfileFieldOption>()
            : await dbContext.PatientProfileFieldOptions.AsNoTracking()
                .Where(option => fieldIds.Contains(option.FieldId))
                .OrderBy(option => option.SortOrder)
                .ThenBy(option => option.Label)
                .ToListAsync(cancellationToken);

        var responseSections = sections
            .Select(section => new PatientProfileSectionResponse(
                section.Id,
                section.Name,
                section.SortOrder,
                section.IsSystem,
                fields
                    .Where(field => field.SectionId == section.Id)
                    .Select(field => new PatientProfileFieldResponse(
                        field.Id,
                        field.Label,
                        field.FieldType.ToString(),
                        field.IsRequired,
                        field.SortOrder,
                        options
                            .Where(option => option.FieldId == field.Id)
                            .Select(option => new PatientProfileFieldOptionResponse(
                                option.Id,
                                option.Label,
                                option.Value,
                                option.SortOrder))
                            .ToList()))
                    .ToList()))
            .ToList();

        return new PatientProfileConfigurationResponse(responseSections);
    }

    public async Task<PatientProfileResponse?> GetPatientProfileAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var patientExists = await dbContext.Patients.AsNoTracking()
            .AnyAsync(patient => patient.Id == patientId, cancellationToken);

        if (!patientExists)
            return null;

        var values = await dbContext.PatientProfileValues.AsNoTracking()
            .Where(value => value.PatientId == patientId)
            .OrderBy(value => value.FieldId)
            .Select(value => new PatientProfileValueResponse(
                value.FieldId,
                value.TextValue,
                value.NumberValue,
                value.BooleanValue,
                value.DateValue,
                value.FileId,
                value.JsonValue))
            .ToListAsync(cancellationToken);

        return new PatientProfileResponse(patientId, values);
    }

    public async Task<PatientProfileResult> SaveAsync(
        SavePatientProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.IsAuthenticated || currentUserContext.UserId is not Guid userId)
            return new PatientProfileResult(PatientProfileOutcome.Unauthenticated);

        var patientExists = await dbContext.Patients.AsNoTracking()
            .AnyAsync(patient => patient.Id == request.PatientId, cancellationToken);

        if (!patientExists)
            return new PatientProfileResult(PatientProfileOutcome.PatientNotFound);

        var requestedFieldIds = request.Values.Select(value => value.FieldId).ToArray();
        if (requestedFieldIds.Distinct().Count() != requestedFieldIds.Length)
        {
            return new PatientProfileResult(
                PatientProfileOutcome.InvalidField,
                Error: "Duplicate patient profile field values are not allowed.");
        }

        var enabledSectionIds = await dbContext.PatientProfileSections.AsNoTracking()
            .Where(section => section.IsEnabled)
            .Select(section => section.Id)
            .ToArrayAsync(cancellationToken);

        var editableFields = enabledSectionIds.Length == 0
            ? new List<PatientProfileField>()
            : await dbContext.PatientProfileFields
                .Where(field =>
                    field.IsEnabled &&
                    enabledSectionIds.Contains(field.SectionId) &&
                    EditableFieldTypes.Contains(field.FieldType))
                .ToListAsync(cancellationToken);

        var editableFieldMap = editableFields.ToDictionary(field => field.Id);
        if (requestedFieldIds.Any(fieldId => !editableFieldMap.ContainsKey(fieldId)))
        {
            return new PatientProfileResult(
                PatientProfileOutcome.InvalidField,
                Error: "One or more patient profile fields are invalid, disabled, or not editable in this workflow.");
        }

        var selectFieldIds = editableFields
            .Where(field => field.FieldType is DynamicFieldType.SingleSelect or DynamicFieldType.MultiSelect)
            .Select(field => field.Id)
            .ToArray();

        var options = selectFieldIds.Length == 0
            ? new List<PatientProfileFieldOption>()
            : await dbContext.PatientProfileFieldOptions.AsNoTracking()
                .Where(option => selectFieldIds.Contains(option.FieldId))
                .ToListAsync(cancellationToken);

        var normalizedValues = new Dictionary<Guid, NormalizedValue>();

        foreach (var submitted in request.Values)
        {
            var field = editableFieldMap[submitted.FieldId];
            var normalized = Normalize(field, submitted, options);

            if (!normalized.Success)
            {
                return new PatientProfileResult(
                    PatientProfileOutcome.InvalidValue,
                    Error: normalized.Error);
            }

            normalizedValues[field.Id] = normalized.Value;
        }

        foreach (var requiredField in editableFields.Where(field => field.IsRequired))
        {
            if (!normalizedValues.TryGetValue(requiredField.Id, out var value) || value.IsEmpty)
            {
                return new PatientProfileResult(
                    PatientProfileOutcome.RequiredValueMissing,
                    Error: $"A value is required for '{requiredField.Label}'.");
            }
        }

        var existingValues = await dbContext.PatientProfileValues
            .Where(value =>
                value.PatientId == request.PatientId &&
                requestedFieldIds.Contains(value.FieldId))
            .ToListAsync(cancellationToken);

        var existingByFieldId = existingValues
            .GroupBy(value => value.FieldId)
            .ToDictionary(group => group.Key, group => group.First());

        var now = DateTime.UtcNow;

        foreach (var submitted in request.Values)
        {
            var normalized = normalizedValues[submitted.FieldId];

            if (normalized.IsEmpty)
            {
                if (existingByFieldId.TryGetValue(submitted.FieldId, out var existingToRemove))
                    dbContext.PatientProfileValues.Remove(existingToRemove);

                continue;
            }

            if (!existingByFieldId.TryGetValue(submitted.FieldId, out var entity))
            {
                entity = new PatientProfileValue
                {
                    Id = Guid.NewGuid(),
                    PatientId = request.PatientId,
                    FieldId = submitted.FieldId,
                    CreatedDate = now,
                    CreateByUserId = userId
                };
                dbContext.PatientProfileValues.Add(entity);
            }
            else
            {
                entity.UpdatedDate = now;
                entity.UpdatedByUserId = userId;
            }

            entity.TextValue = normalized.TextValue;
            entity.NumberValue = normalized.NumberValue;
            entity.BooleanValue = normalized.BooleanValue;
            entity.DateValue = normalized.DateValue;
            entity.JsonValue = normalized.JsonValue;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "PatientProfile.Saved",
            nameof(PatientProfileValue),
            request.PatientId.ToString(),
            new Dictionary<string, object?>
            {
                ["patientId"] = request.PatientId,
                ["fieldCount"] = request.Values.Count
            },
            cancellationToken);

        return new PatientProfileResult(
            PatientProfileOutcome.Success,
            await GetPatientProfileAsync(request.PatientId, cancellationToken));
    }

    private static NormalizeResult Normalize(
        PatientProfileField field,
        SavePatientProfileValueRequest request,
        IReadOnlyCollection<PatientProfileFieldOption> allOptions)
    {
        var hasText = !string.IsNullOrWhiteSpace(request.TextValue);
        var hasNumber = request.NumberValue.HasValue;
        var hasBoolean = request.BooleanValue.HasValue;
        var hasDate = request.DateValue.HasValue;
        var hasJson = !string.IsNullOrWhiteSpace(request.JsonValue);

        switch (field.FieldType)
        {
            case DynamicFieldType.Text:
            case DynamicFieldType.LongText:
                if (hasNumber || hasBoolean || hasDate || hasJson)
                    return NormalizeResult.Fail($"'{field.Label}' expects a text value.");

                return NormalizeResult.Ok(new NormalizedValue(
                    TextValue: Clean(request.TextValue)));

            case DynamicFieldType.Number:
                if (hasText || hasBoolean || hasDate || hasJson)
                    return NormalizeResult.Fail($"'{field.Label}' expects a number value.");

                return NormalizeResult.Ok(new NormalizedValue(
                    NumberValue: request.NumberValue));

            case DynamicFieldType.Boolean:
                if (hasText || hasNumber || hasDate || hasJson)
                    return NormalizeResult.Fail($"'{field.Label}' expects a boolean value.");

                return NormalizeResult.Ok(new NormalizedValue(
                    BooleanValue: request.BooleanValue));

            case DynamicFieldType.Date:
                if (hasText || hasNumber || hasBoolean || hasJson)
                    return NormalizeResult.Fail($"'{field.Label}' expects a date value.");

                return NormalizeResult.Ok(new NormalizedValue(
                    DateValue: request.DateValue));

            case DynamicFieldType.SingleSelect:
                if (hasNumber || hasBoolean || hasDate || hasJson)
                    return NormalizeResult.Fail($"'{field.Label}' expects a single selected option.");

                var selectedValue = Clean(request.TextValue);
                if (selectedValue is null)
                    return NormalizeResult.Ok(new NormalizedValue());

                var allowedSingleValues = allOptions
                    .Where(option => option.FieldId == field.Id)
                    .Select(option => option.Value)
                    .ToHashSet(StringComparer.Ordinal);

                if (!allowedSingleValues.Contains(selectedValue))
                    return NormalizeResult.Fail($"'{field.Label}' contains an unknown selected option.");

                return NormalizeResult.Ok(new NormalizedValue(TextValue: selectedValue));

            case DynamicFieldType.MultiSelect:
                if (hasText || hasNumber || hasBoolean || hasDate)
                    return NormalizeResult.Fail($"'{field.Label}' expects a JSON array of selected option values.");

                if (!hasJson)
                    return NormalizeResult.Ok(new NormalizedValue());

                try
                {
                    var selectedValues = JsonSerializer.Deserialize<string[]>(request.JsonValue!) ?? [];
                    var distinctValues = selectedValues
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Distinct(StringComparer.Ordinal)
                        .ToArray();

                    var allowedMultiValues = allOptions
                        .Where(option => option.FieldId == field.Id)
                        .Select(option => option.Value)
                        .ToHashSet(StringComparer.Ordinal);

                    if (distinctValues.Any(value => !allowedMultiValues.Contains(value)))
                        return NormalizeResult.Fail($"'{field.Label}' contains an unknown selected option.");

                    return NormalizeResult.Ok(new NormalizedValue(
                        JsonValue: distinctValues.Length == 0
                            ? null
                            : JsonSerializer.Serialize(distinctValues)));
                }
                catch (JsonException)
                {
                    return NormalizeResult.Fail($"'{field.Label}' contains invalid multi-select data.");
                }

            default:
                return NormalizeResult.Fail($"'{field.Label}' is not editable in this workflow.");
        }
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record NormalizedValue(
        string? TextValue = null,
        decimal? NumberValue = null,
        bool? BooleanValue = null,
        DateOnly? DateValue = null,
        string? JsonValue = null)
    {
        public bool IsEmpty =>
            TextValue is null &&
            !NumberValue.HasValue &&
            !BooleanValue.HasValue &&
            !DateValue.HasValue &&
            JsonValue is null;
    }

    private sealed record NormalizeResult(
        bool Success,
        NormalizedValue Value,
        string? Error = null)
    {
        public static NormalizeResult Ok(NormalizedValue value) => new(true, value);
        public static NormalizeResult Fail(string error) => new(false, new NormalizedValue(), error);
    }
}
