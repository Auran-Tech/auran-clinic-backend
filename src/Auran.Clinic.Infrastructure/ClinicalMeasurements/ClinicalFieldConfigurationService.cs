using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.ClinicalMeasurements;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.ClinicalMeasurements;

public sealed class ClinicalFieldConfigurationService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IClinicalFieldConfigurationService
{
    public async Task<ClinicalFieldAdminConfigurationResponse> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var fields = await dbContext.ClinicalFields.AsNoTracking()
            .OrderBy(field => field.SortOrder)
            .ThenBy(field => field.Name)
            .ToListAsync(cancellationToken);

        var options = await dbContext.ClinicalFieldOptions.AsNoTracking()
            .OrderBy(option => option.SortOrder)
            .ThenBy(option => option.Label)
            .ToListAsync(cancellationToken);

        var fieldsWithMeasurements = (await dbContext.ClinicalMeasurements.AsNoTracking()
                .Select(measurement => measurement.ClinicalFieldId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();

        return new ClinicalFieldAdminConfigurationResponse(
            fields.Select(field => new ClinicalFieldAdminResponse(
                field.Id,
                field.Name,
                field.FieldType.ToString(),
                field.Unit,
                field.IsEnabled,
                field.SortOrder,
                fieldsWithMeasurements.Contains(field.Id),
                options
                    .Where(option => option.ClinicalFieldId == field.Id)
                    .Select(option => new ClinicalFieldAdminOptionResponse(
                        option.Id,
                        option.Label,
                        option.Value,
                        option.SortOrder))
                    .ToList()))
            .ToList());
    }

    public async Task<ClinicalFieldConfigurationResult> CreateFieldAsync(
        CreateClinicalFieldRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out var clinicId))
            return Unauthenticated();

        if (!Enum.TryParse<DynamicFieldType>(request.FieldType, true, out var fieldType))
            return ValidationError("Unknown dynamic field type.");

        var now = DateTime.UtcNow;
        var field = new ClinicalField
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            Name = request.Name.Trim(),
            FieldType = fieldType,
            Unit = Clean(request.Unit),
            IsEnabled = true,
            SortOrder = request.SortOrder,
            CreatedDate = now,
            CreateByUserId = userId
        };

        dbContext.ClinicalFields.Add(field);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "ClinicalField.Created",
            nameof(ClinicalField),
            field.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["fieldType"] = field.FieldType.ToString(),
                ["unit"] = field.Unit
            },
            cancellationToken);

        return Success(await GetAsync(cancellationToken));
    }

    public async Task<ClinicalFieldConfigurationResult> UpdateFieldAsync(
        UpdateClinicalFieldRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out _))
            return Unauthenticated();

        var field = await dbContext.ClinicalFields
            .SingleOrDefaultAsync(item => item.Id == request.FieldId, cancellationToken);

        if (field is null)
            return NotFound("Clinical field not found.");

        if (!Enum.TryParse<DynamicFieldType>(request.FieldType, true, out var requestedType))
            return ValidationError("Unknown dynamic field type.");

        var hasMeasurements = await dbContext.ClinicalMeasurements.AsNoTracking()
            .AnyAsync(measurement => measurement.ClinicalFieldId == field.Id, cancellationToken);

        if (hasMeasurements && field.FieldType != requestedType)
        {
            return Conflict(
                "Clinical field type cannot be changed after measurements have been recorded.");
        }

        field.Name = request.Name.Trim();
        field.FieldType = requestedType;
        field.Unit = Clean(request.Unit);
        field.IsEnabled = request.IsEnabled;
        field.SortOrder = request.SortOrder;
        field.UpdatedDate = DateTime.UtcNow;
        field.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "ClinicalField.Updated",
            nameof(ClinicalField),
            field.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["fieldType"] = field.FieldType.ToString(),
                ["unit"] = field.Unit,
                ["isEnabled"] = field.IsEnabled,
                ["hasMeasurements"] = hasMeasurements
            },
            cancellationToken);

        return Success(await GetAsync(cancellationToken));
    }

    public async Task<ClinicalFieldConfigurationResult> CreateOptionAsync(
        CreateClinicalFieldOptionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out var clinicId))
            return Unauthenticated();

        var field = await dbContext.ClinicalFields.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.FieldId, cancellationToken);

        if (field is null)
            return NotFound("Clinical field not found.");

        if (field.FieldType is not (DynamicFieldType.SingleSelect or DynamicFieldType.MultiSelect))
            return ValidationError("Options can only be added to select clinical fields.");

        var normalizedValue = request.Value.Trim();
        var duplicate = await dbContext.ClinicalFieldOptions.AsNoTracking()
            .AnyAsync(
                option =>
                    option.ClinicalFieldId == field.Id &&
                    option.Value == normalizedValue,
                cancellationToken);

        if (duplicate)
            return Conflict("An option with this value already exists for the clinical field.");

        var now = DateTime.UtcNow;
        var option = new ClinicalFieldOption
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            ClinicalFieldId = field.Id,
            Label = request.Label.Trim(),
            Value = normalizedValue,
            SortOrder = request.SortOrder,
            CreatedDate = now,
            CreateByUserId = userId
        };

        dbContext.ClinicalFieldOptions.Add(option);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "ClinicalFieldOption.Created",
            nameof(ClinicalFieldOption),
            option.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["fieldId"] = field.Id,
                ["value"] = option.Value
            },
            cancellationToken);

        return Success(await GetAsync(cancellationToken));
    }

    public async Task<ClinicalFieldConfigurationResult> UpdateOptionAsync(
        UpdateClinicalFieldOptionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out _))
            return Unauthenticated();

        var option = await dbContext.ClinicalFieldOptions
            .SingleOrDefaultAsync(item => item.Id == request.OptionId, cancellationToken);

        if (option is null)
            return NotFound("Clinical field option not found.");

        var normalizedValue = request.Value.Trim();
        var valueChanged = !string.Equals(option.Value, normalizedValue, StringComparison.Ordinal);

        if (valueChanged)
        {
            var hasMeasurements = await dbContext.ClinicalMeasurements.AsNoTracking()
                .AnyAsync(
                    measurement => measurement.ClinicalFieldId == option.ClinicalFieldId,
                    cancellationToken);

            if (hasMeasurements)
            {
                return Conflict(
                    "Option values cannot be changed after measurements have been recorded for the clinical field.");
            }

            var duplicate = await dbContext.ClinicalFieldOptions.AsNoTracking()
                .AnyAsync(
                    item =>
                        item.Id != option.Id &&
                        item.ClinicalFieldId == option.ClinicalFieldId &&
                        item.Value == normalizedValue,
                    cancellationToken);

            if (duplicate)
                return Conflict("An option with this value already exists for the clinical field.");
        }

        option.Label = request.Label.Trim();
        option.Value = normalizedValue;
        option.SortOrder = request.SortOrder;
        option.UpdatedDate = DateTime.UtcNow;
        option.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "ClinicalFieldOption.Updated",
            nameof(ClinicalFieldOption),
            option.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["fieldId"] = option.ClinicalFieldId,
                ["value"] = option.Value
            },
            cancellationToken);

        return Success(await GetAsync(cancellationToken));
    }

    public async Task<ClinicalFieldConfigurationResult> DeleteOptionAsync(
        DeleteClinicalFieldOptionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out _, out _))
            return Unauthenticated();

        var option = await dbContext.ClinicalFieldOptions
            .SingleOrDefaultAsync(item => item.Id == request.OptionId, cancellationToken);

        if (option is null)
            return NotFound("Clinical field option not found.");

        var hasMeasurements = await dbContext.ClinicalMeasurements.AsNoTracking()
            .AnyAsync(
                measurement => measurement.ClinicalFieldId == option.ClinicalFieldId,
                cancellationToken);

        if (hasMeasurements)
        {
            return Conflict(
                "Options cannot be deleted after measurements have been recorded for the clinical field.");
        }

        dbContext.ClinicalFieldOptions.Remove(option);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "ClinicalFieldOption.Deleted",
            nameof(ClinicalFieldOption),
            option.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["fieldId"] = option.ClinicalFieldId,
                ["value"] = option.Value
            },
            cancellationToken);

        return Success(await GetAsync(cancellationToken));
    }

    private bool TryGetActor(out Guid userId, out Guid clinicId)
    {
        if (currentUserContext.IsAuthenticated &&
            currentUserContext.UserId is Guid currentUserId &&
            currentUserContext.ClinicId is Guid currentClinicId)
        {
            userId = currentUserId;
            clinicId = currentClinicId;
            return true;
        }

        userId = Guid.Empty;
        clinicId = Guid.Empty;
        return false;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ClinicalFieldConfigurationResult Success(
        ClinicalFieldAdminConfigurationResponse configuration) =>
        new(ClinicalFieldConfigurationOutcome.Success, configuration);

    private static ClinicalFieldConfigurationResult NotFound(string error) =>
        new(ClinicalFieldConfigurationOutcome.NotFound, Error: error);

    private static ClinicalFieldConfigurationResult ValidationError(string error) =>
        new(ClinicalFieldConfigurationOutcome.ValidationError, Error: error);

    private static ClinicalFieldConfigurationResult Conflict(string error) =>
        new(ClinicalFieldConfigurationOutcome.Conflict, Error: error);

    private static ClinicalFieldConfigurationResult Unauthenticated() =>
        new(ClinicalFieldConfigurationOutcome.Unauthenticated);
}
