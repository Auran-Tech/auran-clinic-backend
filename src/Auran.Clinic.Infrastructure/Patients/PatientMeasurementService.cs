using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Patients;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Patients;

public sealed class PatientMeasurementService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IPatientMeasurementService
{
    public async Task<PatientMeasurementsResponse?> GetAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var patientExists = await dbContext.Patients
            .AsNoTracking()
            .AnyAsync(patient => patient.Id == patientId, cancellationToken);
        if (!patientExists)
            return null;

        var fields = await dbContext.ClinicalFields
            .AsNoTracking()
            .Where(field => field.IsEnabled)
            .OrderBy(field => field.SortOrder)
            .ThenBy(field => field.Name)
            .ToListAsync(cancellationToken);

        var fieldIds = fields.Select(field => field.Id).ToArray();

        var options = await dbContext.ClinicalFieldOptions
            .AsNoTracking()
            .Where(option => fieldIds.Contains(option.ClinicalFieldId))
            .OrderBy(option => option.SortOrder)
            .ToListAsync(cancellationToken);

        var measurements = await dbContext.ClinicalMeasurements
            .AsNoTracking()
            .Where(measurement => measurement.PatientId == patientId)
            .OrderByDescending(measurement => measurement.RecordedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        var fieldMap = fields.ToDictionary(field => field.Id);

        return new PatientMeasurementsResponse(
            patientId,
            fields.Select(field => new ClinicalFieldResponse(
                field.Id,
                field.Name,
                field.FieldType,
                field.Unit,
                field.SortOrder,
                options
                    .Where(option => option.ClinicalFieldId == field.Id)
                    .Select(option => new ClinicalFieldOptionResponse(
                        option.Id,
                        option.Label,
                        option.Value,
                        option.SortOrder))
                    .ToList()))
                .ToList(),
            measurements
                .Where(measurement => fieldMap.ContainsKey(measurement.ClinicalFieldId))
                .Select(measurement =>
                {
                    var field = fieldMap[measurement.ClinicalFieldId];
                    return new ClinicalMeasurementResponse(
                        measurement.Id,
                        measurement.ClinicalFieldId,
                        field.Name,
                        field.Unit,
                        measurement.TextValue,
                        measurement.NumberValue,
                        measurement.BooleanValue,
                        measurement.DateValue,
                        measurement.JsonValue,
                        measurement.RecordedAtUtc);
                })
                .ToList());
    }

    public async Task<ClinicalMeasurementResponse?> AddAsync(
        AddClinicalMeasurementRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out var clinicId))
            return null;

        var patientExists = await dbContext.Patients
            .AnyAsync(patient => patient.Id == request.PatientId, cancellationToken);
        if (!patientExists)
            return null;

        var field = await dbContext.ClinicalFields
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.ClinicalFieldId && item.IsEnabled, cancellationToken);
        if (field is null)
            return null;

        var now = DateTime.UtcNow;
        var measurement = new ClinicalMeasurement
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            PatientId = request.PatientId,
            ClinicalFieldId = request.ClinicalFieldId,
            TextValue = Clean(request.TextValue),
            NumberValue = request.NumberValue,
            BooleanValue = request.BooleanValue,
            DateValue = request.DateValue,
            JsonValue = Clean(request.JsonValue),
            RecordedAtUtc = now,
            RecordedByUserId = userId,
            CreatedDate = now,
            CreateByUserId = userId
        };

        dbContext.ClinicalMeasurements.Add(measurement);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "Patient.MeasurementAdded",
            nameof(ClinicalMeasurement),
            measurement.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["patientId"] = request.PatientId,
                ["clinicalFieldId"] = request.ClinicalFieldId
            },
            cancellationToken);

        return new ClinicalMeasurementResponse(
            measurement.Id,
            measurement.ClinicalFieldId,
            field.Name,
            field.Unit,
            measurement.TextValue,
            measurement.NumberValue,
            measurement.BooleanValue,
            measurement.DateValue,
            measurement.JsonValue,
            measurement.RecordedAtUtc);
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
}
