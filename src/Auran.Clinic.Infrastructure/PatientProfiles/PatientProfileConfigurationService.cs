using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.PatientProfiles;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.PatientProfiles;

public sealed class PatientProfileConfigurationService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IPatientProfileConfigurationService
{
    public async Task<PatientProfileAdminConfigurationResponse> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var sections = await dbContext.PatientProfileSections.AsNoTracking()
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Name)
            .ToListAsync(cancellationToken);

        var fields = await dbContext.PatientProfileFields.AsNoTracking()
            .OrderBy(field => field.SortOrder)
            .ThenBy(field => field.Label)
            .ToListAsync(cancellationToken);

        var options = await dbContext.PatientProfileFieldOptions.AsNoTracking()
            .OrderBy(option => option.SortOrder)
            .ThenBy(option => option.Label)
            .ToListAsync(cancellationToken);

        var fieldIdsWithValues = (await dbContext.PatientProfileValues.AsNoTracking()
                .Select(value => value.FieldId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();

        return new PatientProfileAdminConfigurationResponse(
            sections.Select(section => new PatientProfileAdminSectionResponse(
                section.Id,
                section.Name,
                section.SortOrder,
                section.IsSystem,
                section.IsEnabled,
                fields
                    .Where(field => field.SectionId == section.Id)
                    .Select(field => new PatientProfileAdminFieldResponse(
                        field.Id,
                        field.SectionId,
                        field.Label,
                        field.FieldType.ToString(),
                        field.IsRequired,
                        field.IsEnabled,
                        field.SortOrder,
                        fieldIdsWithValues.Contains(field.Id),
                        options
                            .Where(option => option.FieldId == field.Id)
                            .Select(option => new PatientProfileAdminOptionResponse(
                                option.Id,
                                option.Label,
                                option.Value,
                                option.SortOrder))
                            .ToList()))
                    .ToList()))
            .ToList());
    }

    public async Task<PatientProfileConfigurationResult> CreateSectionAsync(
        CreatePatientProfileSectionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out var clinicId))
            return Unauthenticated();

        var now = DateTime.UtcNow;
        var section = new PatientProfileSection
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            Name = request.Name.Trim(),
            SortOrder = request.SortOrder,
            IsSystem = false,
            IsEnabled = true,
            CreatedDate = now,
            CreateByUserId = userId
        };

        dbContext.PatientProfileSections.Add(section);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "PatientProfileSection.Created",
            nameof(PatientProfileSection),
            section.Id.ToString(),
            new Dictionary<string, object?> { ["name"] = section.Name },
            cancellationToken);

        return Success(await GetAsync(cancellationToken));
    }

    public async Task<PatientProfileConfigurationResult> UpdateSectionAsync(
        UpdatePatientProfileSectionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out _))
            return Unauthenticated();

        var section = await dbContext.PatientProfileSections
            .SingleOrDefaultAsync(item => item.Id == request.SectionId, cancellationToken);

        if (section is null)
            return NotFound("Patient profile section not found.");

        if (section.IsSystem && !request.IsEnabled)
            return ValidationError("System patient profile sections cannot be disabled.");

        section.Name = request.Name.Trim();
        section.SortOrder = request.SortOrder;
        section.IsEnabled = request.IsEnabled;
        section.UpdatedDate = DateTime.UtcNow;
        section.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "PatientProfileSection.Updated",
            nameof(PatientProfileSection),
            section.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["name"] = section.Name,
                ["isEnabled"] = section.IsEnabled
            },
            cancellationToken);

        return Success(await GetAsync(cancellationToken));
    }

    public async Task<PatientProfileConfigurationResult> CreateFieldAsync(
        CreatePatientProfileFieldRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out var clinicId))
            return Unauthenticated();

        var sectionExists = await dbContext.PatientProfileSections.AsNoTracking()
            .AnyAsync(section => section.Id == request.SectionId, cancellationToken);

        if (!sectionExists)
            return NotFound("Patient profile section not found.");

        if (!Enum.TryParse<DynamicFieldType>(request.FieldType, true, out var fieldType))
            return ValidationError("Unknown dynamic field type.");

        var now = DateTime.UtcNow;
        var field = new PatientProfileField
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            SectionId = request.SectionId,
            Label = request.Label.Trim(),
            FieldType = fieldType,
            IsRequired = request.IsRequired,
            IsEnabled = true,
            SortOrder = request.SortOrder,
            CreatedDate = now,
            CreateByUserId = userId
        };

        dbContext.PatientProfileFields.Add(field);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "PatientProfileField.Created",
            nameof(PatientProfileField),
            field.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["sectionId"] = field.SectionId,
                ["fieldType"] = field.FieldType.ToString()
            },
            cancellationToken);

        return Success(await GetAsync(cancellationToken));
    }

    public async Task<PatientProfileConfigurationResult> UpdateFieldAsync(
        UpdatePatientProfileFieldRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out _))
            return Unauthenticated();

        var field = await dbContext.PatientProfileFields
            .SingleOrDefaultAsync(item => item.Id == request.FieldId, cancellationToken);

        if (field is null)
            return NotFound("Patient profile field not found.");

        var targetSectionExists = await dbContext.PatientProfileSections.AsNoTracking()
            .AnyAsync(section => section.Id == request.SectionId, cancellationToken);

        if (!targetSectionExists)
            return NotFound("Patient profile section not found.");

        if (!Enum.TryParse<DynamicFieldType>(request.FieldType, true, out var requestedType))
            return ValidationError("Unknown dynamic field type.");

        var hasValues = await dbContext.PatientProfileValues.AsNoTracking()
            .AnyAsync(value => value.FieldId == field.Id, cancellationToken);

        if (hasValues && field.FieldType != requestedType)
        {
            return Conflict(
                "Field type cannot be changed after patient values have been recorded.");
        }

        field.SectionId = request.SectionId;
        field.Label = request.Label.Trim();
        field.FieldType = requestedType;
        field.IsRequired = request.IsRequired;
        field.IsEnabled = request.IsEnabled;
        field.SortOrder = request.SortOrder;
        field.UpdatedDate = DateTime.UtcNow;
        field.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "PatientProfileField.Updated",
            nameof(PatientProfileField),
            field.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["sectionId"] = field.SectionId,
                ["fieldType"] = field.FieldType.ToString(),
                ["isEnabled"] = field.IsEnabled,
                ["hasValues"] = hasValues
            },
            cancellationToken);

        return Success(await GetAsync(cancellationToken));
    }

    public async Task<PatientProfileConfigurationResult> CreateOptionAsync(
        CreatePatientProfileOptionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out var clinicId))
            return Unauthenticated();

        var field = await dbContext.PatientProfileFields.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.FieldId, cancellationToken);

        if (field is null)
            return NotFound("Patient profile field not found.");

        if (field.FieldType is not (DynamicFieldType.SingleSelect or DynamicFieldType.MultiSelect))
            return ValidationError("Options can only be added to select fields.");

        var normalizedValue = request.Value.Trim();
        var duplicate = await dbContext.PatientProfileFieldOptions.AsNoTracking()
            .AnyAsync(
                option => option.FieldId == field.Id && option.Value == normalizedValue,
                cancellationToken);

        if (duplicate)
            return Conflict("An option with this value already exists for the field.");

        var now = DateTime.UtcNow;
        var option = new PatientProfileFieldOption
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            FieldId = field.Id,
            Label = request.Label.Trim(),
            Value = normalizedValue,
            SortOrder = request.SortOrder,
            CreatedDate = now,
            CreateByUserId = userId
        };

        dbContext.PatientProfileFieldOptions.Add(option);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "PatientProfileOption.Created",
            nameof(PatientProfileFieldOption),
            option.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["fieldId"] = field.Id,
                ["value"] = option.Value
            },
            cancellationToken);

        return Success(await GetAsync(cancellationToken));
    }

    public async Task<PatientProfileConfigurationResult> UpdateOptionAsync(
        UpdatePatientProfileOptionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out _))
            return Unauthenticated();

        var option = await dbContext.PatientProfileFieldOptions
            .SingleOrDefaultAsync(item => item.Id == request.OptionId, cancellationToken);

        if (option is null)
            return NotFound("Patient profile option not found.");

        var field = await dbContext.PatientProfileFields.AsNoTracking()
            .SingleAsync(item => item.Id == option.FieldId, cancellationToken);

        var normalizedValue = request.Value.Trim();
        var valueChanged = !string.Equals(option.Value, normalizedValue, StringComparison.Ordinal);

        if (valueChanged)
        {
            var fieldHasValues = await dbContext.PatientProfileValues.AsNoTracking()
                .AnyAsync(value => value.FieldId == field.Id, cancellationToken);

            if (fieldHasValues)
            {
                return Conflict(
                    "Option values cannot be changed after patient values have been recorded for the field.");
            }

            var duplicate = await dbContext.PatientProfileFieldOptions.AsNoTracking()
                .AnyAsync(
                    item =>
                        item.Id != option.Id &&
                        item.FieldId == option.FieldId &&
                        item.Value == normalizedValue,
                    cancellationToken);

            if (duplicate)
                return Conflict("An option with this value already exists for the field.");
        }

        option.Label = request.Label.Trim();
        option.Value = normalizedValue;
        option.SortOrder = request.SortOrder;
        option.UpdatedDate = DateTime.UtcNow;
        option.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "PatientProfileOption.Updated",
            nameof(PatientProfileFieldOption),
            option.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["fieldId"] = option.FieldId,
                ["value"] = option.Value
            },
            cancellationToken);

        return Success(await GetAsync(cancellationToken));
    }

    public async Task<PatientProfileConfigurationResult> DeleteOptionAsync(
        DeletePatientProfileOptionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out _, out _))
            return Unauthenticated();

        var option = await dbContext.PatientProfileFieldOptions
            .SingleOrDefaultAsync(item => item.Id == request.OptionId, cancellationToken);

        if (option is null)
            return NotFound("Patient profile option not found.");

        var fieldHasValues = await dbContext.PatientProfileValues.AsNoTracking()
            .AnyAsync(value => value.FieldId == option.FieldId, cancellationToken);

        if (fieldHasValues)
        {
            return Conflict(
                "Options cannot be deleted after patient values have been recorded for the field.");
        }

        dbContext.PatientProfileFieldOptions.Remove(option);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "PatientProfileOption.Deleted",
            nameof(PatientProfileFieldOption),
            option.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["fieldId"] = option.FieldId,
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

    private static PatientProfileConfigurationResult Success(
        PatientProfileAdminConfigurationResponse configuration) =>
        new(PatientProfileConfigurationOutcome.Success, configuration);

    private static PatientProfileConfigurationResult NotFound(string error) =>
        new(PatientProfileConfigurationOutcome.NotFound, Error: error);

    private static PatientProfileConfigurationResult ValidationError(string error) =>
        new(PatientProfileConfigurationOutcome.ValidationError, Error: error);

    private static PatientProfileConfigurationResult Conflict(string error) =>
        new(PatientProfileConfigurationOutcome.Conflict, Error: error);

    private static PatientProfileConfigurationResult Unauthenticated() =>
        new(PatientProfileConfigurationOutcome.Unauthenticated);
}
