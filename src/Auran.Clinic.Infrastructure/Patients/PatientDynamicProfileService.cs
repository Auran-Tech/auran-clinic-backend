using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Patients;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Patients;

public sealed class PatientDynamicProfileService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IPatientDynamicProfileService
{
    public async Task<PatientDynamicProfileResponse?> GetAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var patientExists = await dbContext.Patients
            .AsNoTracking()
            .AnyAsync(patient => patient.Id == patientId, cancellationToken);
        if (!patientExists)
            return null;

        var sections = await dbContext.PatientProfileSections
            .AsNoTracking()
            .Where(section => section.IsEnabled)
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Name)
            .ToListAsync(cancellationToken);

        var fields = await dbContext.PatientProfileFields
            .AsNoTracking()
            .Where(field => field.IsEnabled)
            .OrderBy(field => field.SortOrder)
            .ThenBy(field => field.Label)
            .ToListAsync(cancellationToken);

        var fieldIds = fields.Select(field => field.Id).ToArray();

        var options = await dbContext.PatientProfileFieldOptions
            .AsNoTracking()
            .Where(option => fieldIds.Contains(option.FieldId))
            .OrderBy(option => option.SortOrder)
            .ToListAsync(cancellationToken);

        var values = await dbContext.PatientProfileValues
            .AsNoTracking()
            .Where(value => value.PatientId == patientId && fieldIds.Contains(value.FieldId))
            .ToListAsync(cancellationToken);

        var sectionResponses = sections
            .Select(section => new PatientDynamicSectionResponse(
                section.Id,
                section.Name,
                section.SortOrder,
                fields
                    .Where(field => field.SectionId == section.Id)
                    .Select(field =>
                    {
                        var value = values.FirstOrDefault(item => item.FieldId == field.Id);
                        return new PatientDynamicFieldResponse(
                            field.Id,
                            field.Label,
                            field.FieldType,
                            field.IsRequired,
                            field.SortOrder,
                            value?.TextValue,
                            value?.NumberValue,
                            value?.BooleanValue,
                            value?.DateValue,
                            value?.JsonValue,
                            options
                                .Where(option => option.FieldId == field.Id)
                                .Select(option => new DynamicFieldOptionResponse(
                                    option.Id,
                                    option.Label,
                                    option.Value,
                                    option.SortOrder))
                                .ToList());
                    })
                    .ToList()))
            .ToList();

        return new PatientDynamicProfileResponse(patientId, sectionResponses);
    }

    public async Task<bool> SaveValueAsync(
        SavePatientDynamicValueRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId))
            return false;

        var patientExists = await dbContext.Patients
            .AnyAsync(patient => patient.Id == request.PatientId, cancellationToken);
        if (!patientExists)
            return false;

        var field = await dbContext.PatientProfileFields
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.FieldId && item.IsEnabled, cancellationToken);
        if (field is null)
            return false;

        var value = await dbContext.PatientProfileValues
            .SingleOrDefaultAsync(
                item => item.PatientId == request.PatientId && item.FieldId == request.FieldId,
                cancellationToken);

        var now = DateTime.UtcNow;

        if (value is null)
        {
            value = new PatientProfileValue
            {
                Id = Guid.NewGuid(),
                PatientId = request.PatientId,
                FieldId = request.FieldId,
                CreatedDate = now,
                CreateByUserId = userId
            };
            dbContext.PatientProfileValues.Add(value);
        }
        else
        {
            value.UpdatedDate = now;
            value.UpdatedByUserId = userId;
        }

        value.TextValue = Clean(request.TextValue);
        value.NumberValue = request.NumberValue;
        value.BooleanValue = request.BooleanValue;
        value.DateValue = request.DateValue;
        value.JsonValue = Clean(request.JsonValue);

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "Patient.DynamicProfileValueSaved",
            nameof(PatientProfileValue),
            value.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["patientId"] = request.PatientId,
                ["fieldId"] = request.FieldId
            },
            cancellationToken);

        return true;
    }

    private bool TryGetActor(out Guid userId)
    {
        if (currentUserContext.IsAuthenticated && currentUserContext.UserId is Guid currentUserId)
        {
            userId = currentUserId;
            return true;
        }

        userId = Guid.Empty;
        return false;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
