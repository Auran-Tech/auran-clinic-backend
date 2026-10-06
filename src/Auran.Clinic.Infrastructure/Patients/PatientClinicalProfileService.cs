using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Patients;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Patients;

public sealed class PatientClinicalProfileService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IPatientClinicalProfileService
{
    public async Task<PatientClinicalProfileResponse?> GetAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var patient = await dbContext.Patients
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == patientId, cancellationToken);
        if (patient is null)
            return null;

        var allergies = await dbContext.PatientAllergies
            .AsNoTracking()
            .Where(item => item.PatientId == patientId)
            .OrderByDescending(item => item.RecordedAtUtc)
            .Select(item => new PatientAllergyResponse(
                item.Id,
                item.Name,
                item.Reaction,
                item.Notes,
                item.RecordedAtUtc))
            .ToListAsync(cancellationToken);

        var conditions = await dbContext.PatientConditions
            .AsNoTracking()
            .Where(item => item.PatientId == patientId)
            .OrderByDescending(item => item.RecordedAtUtc)
            .Select(item => new PatientConditionResponse(
                item.Id,
                item.Name,
                item.Notes,
                item.RecordedAtUtc))
            .ToListAsync(cancellationToken);

        var medications = await dbContext.PatientMedications
            .AsNoTracking()
            .Where(item => item.PatientId == patientId)
            .OrderByDescending(item => item.RecordedAtUtc)
            .Select(item => new PatientMedicationResponse(
                item.Id,
                item.Name,
                item.Dosage,
                item.Notes,
                item.RecordedAtUtc))
            .ToListAsync(cancellationToken);

        return new PatientClinicalProfileResponse(
            new PatientResponse(
                patient.Id,
                patient.PatientNumber,
                patient.FullName,
                patient.Phone,
                patient.Gender,
                patient.DateOfBirth,
                patient.Notes,
                patient.CreatedDate,
                patient.UpdatedDate),
            allergies,
            conditions,
            medications);
    }

    public async Task<PatientAllergyResponse?> AddAllergyAsync(
        AddPatientAllergyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out var clinicId))
            return null;
        if (!await dbContext.Patients.AnyAsync(item => item.Id == request.PatientId, cancellationToken))
            return null;

        var now = DateTime.UtcNow;
        var entity = new PatientAllergy
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            PatientId = request.PatientId,
            Name = request.Name.Trim(),
            Reaction = Clean(request.Reaction),
            Notes = Clean(request.Notes),
            RecordedAtUtc = now,
            RecordedByUserId = userId,
            CreatedDate = now,
            CreateByUserId = userId
        };

        dbContext.PatientAllergies.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(
            "Patient.AllergyAdded",
            nameof(PatientAllergy),
            entity.Id.ToString(),
            new Dictionary<string, object?> { ["patientId"] = request.PatientId, ["name"] = entity.Name },
            cancellationToken);

        return new PatientAllergyResponse(entity.Id, entity.Name, entity.Reaction, entity.Notes, entity.RecordedAtUtc);
    }

    public async Task<PatientConditionResponse?> AddConditionAsync(
        AddPatientConditionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out var clinicId))
            return null;
        if (!await dbContext.Patients.AnyAsync(item => item.Id == request.PatientId, cancellationToken))
            return null;

        var now = DateTime.UtcNow;
        var entity = new PatientCondition
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            PatientId = request.PatientId,
            Name = request.Name.Trim(),
            Notes = Clean(request.Notes),
            RecordedAtUtc = now,
            RecordedByUserId = userId,
            CreatedDate = now,
            CreateByUserId = userId
        };

        dbContext.PatientConditions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(
            "Patient.ConditionAdded",
            nameof(PatientCondition),
            entity.Id.ToString(),
            new Dictionary<string, object?> { ["patientId"] = request.PatientId, ["name"] = entity.Name },
            cancellationToken);

        return new PatientConditionResponse(entity.Id, entity.Name, entity.Notes, entity.RecordedAtUtc);
    }

    public async Task<PatientMedicationResponse?> AddMedicationAsync(
        AddPatientMedicationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out var clinicId))
            return null;
        if (!await dbContext.Patients.AnyAsync(item => item.Id == request.PatientId, cancellationToken))
            return null;

        var now = DateTime.UtcNow;
        var entity = new PatientMedication
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            PatientId = request.PatientId,
            Name = request.Name.Trim(),
            Dosage = Clean(request.Dosage),
            Notes = Clean(request.Notes),
            RecordedAtUtc = now,
            RecordedByUserId = userId,
            CreatedDate = now,
            CreateByUserId = userId
        };

        dbContext.PatientMedications.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(
            "Patient.MedicationAdded",
            nameof(PatientMedication),
            entity.Id.ToString(),
            new Dictionary<string, object?> { ["patientId"] = request.PatientId, ["name"] = entity.Name },
            cancellationToken);

        return new PatientMedicationResponse(entity.Id, entity.Name, entity.Dosage, entity.Notes, entity.RecordedAtUtc);
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
