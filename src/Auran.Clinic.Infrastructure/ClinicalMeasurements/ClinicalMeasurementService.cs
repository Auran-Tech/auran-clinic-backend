using System.Text.Json;
using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.ClinicalMeasurements;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.ClinicalMeasurements;

public sealed class ClinicalMeasurementService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IClinicalMeasurementService
{
    private static readonly DynamicFieldType[] EditableTypes =
    [
        DynamicFieldType.Text,
        DynamicFieldType.LongText,
        DynamicFieldType.Number,
        DynamicFieldType.Boolean,
        DynamicFieldType.Date,
        DynamicFieldType.SingleSelect,
        DynamicFieldType.MultiSelect
    ];

    public async Task<IReadOnlyList<ClinicalMeasurementFieldResponse>> ListFieldsAsync(
        CancellationToken cancellationToken = default)
    {
        var fields = await dbContext.ClinicalFields.AsNoTracking()
            .Where(field => field.IsEnabled)
            .OrderBy(field => field.SortOrder)
            .ThenBy(field => field.Name)
            .ToListAsync(cancellationToken);

        var fieldIds = fields.Select(field => field.Id).ToArray();
        var options = fieldIds.Length == 0
            ? new List<ClinicalFieldOption>()
            : await dbContext.ClinicalFieldOptions.AsNoTracking()
                .Where(option => fieldIds.Contains(option.ClinicalFieldId))
                .OrderBy(option => option.SortOrder)
                .ThenBy(option => option.Label)
                .ToListAsync(cancellationToken);

        return fields
            .Select(field => new ClinicalMeasurementFieldResponse(
                field.Id,
                field.Name,
                field.FieldType.ToString(),
                field.Unit,
                field.SortOrder,
                options
                    .Where(option => option.ClinicalFieldId == field.Id)
                    .Select(option => new ClinicalMeasurementFieldOptionResponse(
                        option.Id,
                        option.Label,
                        option.Value,
                        option.SortOrder))
                    .ToList()))
            .ToList();
    }

    public async Task<IReadOnlyList<ClinicalMeasurementResponse>?> ListForVisitAsync(
        Guid visitId,
        CancellationToken cancellationToken = default)
    {
        var visitExists = await dbContext.Visits.AsNoTracking()
            .AnyAsync(visit => visit.Id == visitId, cancellationToken);

        if (!visitExists)
            return null;

        return await (
            from measurement in dbContext.ClinicalMeasurements.AsNoTracking()
            join field in dbContext.ClinicalFields.AsNoTracking()
                on measurement.ClinicalFieldId equals field.Id
            where measurement.VisitId == visitId
            orderby measurement.RecordedAtUtc descending
            select new ClinicalMeasurementResponse(
                measurement.Id,
                measurement.PatientId,
                measurement.VisitId,
                measurement.ClinicalFieldId,
                field.Name,
                field.FieldType.ToString(),
                field.Unit,
                measurement.TextValue,
                measurement.NumberValue,
                measurement.BooleanValue,
                measurement.DateValue,
                measurement.JsonValue,
                measurement.RecordedAtUtc,
                measurement.RecordedByUserId))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClinicalMeasurementResult> RecordAsync(
        RecordClinicalMeasurementsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.IsAuthenticated || currentUserContext.UserId is not Guid userId)
            return new ClinicalMeasurementResult(ClinicalMeasurementOutcome.Unauthenticated);

        var visit = await dbContext.Visits
            .SingleOrDefaultAsync(item => item.Id == request.VisitId, cancellationToken);

        if (visit is null)
            return new ClinicalMeasurementResult(ClinicalMeasurementOutcome.VisitNotFound);

        if (visit.Status != VisitStatus.Open)
            return new ClinicalMeasurementResult(ClinicalMeasurementOutcome.VisitClosed);

        if (visit.DoctorId != userId && !currentUserContext.IsSuperUser)
            return new ClinicalMeasurementResult(ClinicalMeasurementOutcome.Forbidden);

        var requestedFieldIds = request.Values.Select(value => value.FieldId).ToArray();
        if (requestedFieldIds.Distinct().Count() != requestedFieldIds.Length)
        {
            return new ClinicalMeasurementResult(
                ClinicalMeasurementOutcome.InvalidField,
                Error: "Duplicate clinical measurement fields are not allowed in one batch.");
        }

        var fields = requestedFieldIds.Length == 0
            ? new List<ClinicalField>()
            : await dbContext.ClinicalFields
                .Where(field =>
                    requestedFieldIds.Contains(field.Id) &&
                    field.IsEnabled &&
                    EditableTypes.Contains(field.FieldType))
                .ToListAsync(cancellationToken);

        if (fields.Count != requestedFieldIds.Length)
        {
            return new ClinicalMeasurementResult(
                ClinicalMeasurementOutcome.InvalidField,
                Error: "One or more clinical fields are invalid, disabled, or not recordable.");
        }

        var fieldMap = fields.ToDictionary(field => field.Id);

        var selectFieldIds = fields
            .Where(field => field.FieldType is DynamicFieldType.SingleSelect or DynamicFieldType.MultiSelect)
            .Select(field => field.Id)
            .ToArray();

        var options = selectFieldIds.Length == 0
            ? new List<ClinicalFieldOption>()
            : await dbContext.ClinicalFieldOptions.AsNoTracking()
                .Where(option => selectFieldIds.Contains(option.ClinicalFieldId))
                .ToListAsync(cancellationToken);

        var normalized = new List<(ClinicalField Field, NormalizedValue Value)>();

        foreach (var submitted in request.Values)
        {
            var field = fieldMap[submitted.FieldId];
            var result = Normalize(field, submitted, options);

            if (!result.Success)
            {
                return new ClinicalMeasurementResult(
                    ClinicalMeasurementOutcome.InvalidValue,
                    Error: result.Error);
            }

            normalized.Add((field, result.Value));
        }

        var now = DateTime.UtcNow;
        var entities = normalized
            .Select(item => new ClinicalMeasurement
            {
                Id = Guid.NewGuid(),
                PatientId = visit.PatientId,
                VisitId = visit.Id,
                ClinicalFieldId = item.Field.Id,
                TextValue = item.Value.TextValue,
                NumberValue = item.Value.NumberValue,
                BooleanValue = item.Value.BooleanValue,
                DateValue = item.Value.DateValue,
                JsonValue = item.Value.JsonValue,
                RecordedAtUtc = now,
                RecordedByUserId = userId,
                CreatedDate = now,
                CreateByUserId = userId
            })
            .ToList();

        dbContext.ClinicalMeasurements.AddRange(entities);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "ClinicalMeasurement.Recorded",
            nameof(ClinicalMeasurement),
            visit.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["visitId"] = visit.Id,
                ["patientId"] = visit.PatientId,
                ["measurementCount"] = entities.Count
            },
            cancellationToken);

        return new ClinicalMeasurementResult(
            ClinicalMeasurementOutcome.Success,
            await ListForVisitAsync(visit.Id, cancellationToken));
    }

    private static NormalizeResult Normalize(
        ClinicalField field,
        RecordClinicalMeasurementValueRequest request,
        IReadOnlyCollection<ClinicalFieldOption> allOptions)
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
                if (!hasText || hasNumber || hasBoolean || hasDate || hasJson)
                    return NormalizeResult.Fail($"'{field.Name}' expects a text value.");

                return NormalizeResult.Ok(new NormalizedValue(
                    TextValue: request.TextValue!.Trim()));

            case DynamicFieldType.Number:
                if (!hasNumber || hasText || hasBoolean || hasDate || hasJson)
                    return NormalizeResult.Fail($"'{field.Name}' expects a number value.");

                return NormalizeResult.Ok(new NormalizedValue(
                    NumberValue: request.NumberValue));

            case DynamicFieldType.Boolean:
                if (!hasBoolean || hasText || hasNumber || hasDate || hasJson)
                    return NormalizeResult.Fail($"'{field.Name}' expects a boolean value.");

                return NormalizeResult.Ok(new NormalizedValue(
                    BooleanValue: request.BooleanValue));

            case DynamicFieldType.Date:
                if (!hasDate || hasText || hasNumber || hasBoolean || hasJson)
                    return NormalizeResult.Fail($"'{field.Name}' expects a date value.");

                return NormalizeResult.Ok(new NormalizedValue(
                    DateValue: request.DateValue));

            case DynamicFieldType.SingleSelect:
                if (!hasText || hasNumber || hasBoolean || hasDate || hasJson)
                    return NormalizeResult.Fail($"'{field.Name}' expects one configured option.");

                var selected = request.TextValue!.Trim();
                var allowedSingle = allOptions
                    .Where(option => option.ClinicalFieldId == field.Id)
                    .Select(option => option.Value)
                    .ToHashSet(StringComparer.Ordinal);

                if (!allowedSingle.Contains(selected))
                    return NormalizeResult.Fail($"'{field.Name}' contains an unknown option.");

                return NormalizeResult.Ok(new NormalizedValue(TextValue: selected));

            case DynamicFieldType.MultiSelect:
                if (!hasJson || hasText || hasNumber || hasBoolean || hasDate)
                    return NormalizeResult.Fail($"'{field.Name}' expects a non-empty option array.");

                try
                {
                    var values = JsonSerializer.Deserialize<string[]>(request.JsonValue!) ?? [];
                    var distinct = values
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Distinct(StringComparer.Ordinal)
                        .ToArray();

                    if (distinct.Length == 0)
                        return NormalizeResult.Fail($"'{field.Name}' expects at least one option.");

                    var allowedMulti = allOptions
                        .Where(option => option.ClinicalFieldId == field.Id)
                        .Select(option => option.Value)
                        .ToHashSet(StringComparer.Ordinal);

                    if (distinct.Any(value => !allowedMulti.Contains(value)))
                        return NormalizeResult.Fail($"'{field.Name}' contains an unknown option.");

                    return NormalizeResult.Ok(new NormalizedValue(
                        JsonValue: JsonSerializer.Serialize(distinct)));
                }
                catch (JsonException)
                {
                    return NormalizeResult.Fail($"'{field.Name}' contains invalid option data.");
                }

            default:
                return NormalizeResult.Fail($"'{field.Name}' cannot be recorded as a clinical measurement.");
        }
    }

    private sealed record NormalizedValue(
        string? TextValue = null,
        decimal? NumberValue = null,
        bool? BooleanValue = null,
        DateOnly? DateValue = null,
        string? JsonValue = null);

    private sealed record NormalizeResult(
        bool Success,
        NormalizedValue Value,
        string? Error = null)
    {
        public static NormalizeResult Ok(NormalizedValue value) => new(true, value);
        public static NormalizeResult Fail(string error) => new(false, new NormalizedValue(), error);
    }
}
