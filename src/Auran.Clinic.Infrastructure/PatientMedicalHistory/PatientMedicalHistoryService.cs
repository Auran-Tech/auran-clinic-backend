using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.PatientMedicalHistory;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.PatientMedicalHistory;

public sealed class PatientMedicalHistoryService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IPatientMedicalHistoryService
{
    public async Task<PatientMedicalHistoryResponse?> GetAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Patients.AsNoTracking()
            .AnyAsync(patient => patient.Id == patientId, cancellationToken);

        if (!exists)
            return null;

        var conditions = await dbContext.PatientConditions.AsNoTracking()
            .Where(item => item.PatientId == patientId)
            .OrderByDescending(item => item.RecordedAtUtc)
            .Select(item => new PatientConditionResponse(
                item.Id,
                item.Name,
                item.Notes,
                item.RecordedAtUtc,
                item.RecordedByUserId))
            .ToListAsync(cancellationToken);

        var allergies = await dbContext.PatientAllergies.AsNoTracking()
            .Where(item => item.PatientId == patientId)
            .OrderByDescending(item => item.RecordedAtUtc)
            .Select(item => new PatientAllergyResponse(
                item.Id,
                item.Name,
                item.Reaction,
                item.Notes,
                item.RecordedAtUtc,
                item.RecordedByUserId))
            .ToListAsync(cancellationToken);

        var medications = await dbContext.PatientMedications.AsNoTracking()
            .Where(item => item.PatientId == patientId)
            .OrderByDescending(item => item.RecordedAtUtc)
            .Select(item => new PatientMedicationResponse(
                item.Id,
                item.Name,
                item.Dosage,
                item.Notes,
                item.RecordedAtUtc,
                item.RecordedByUserId))
            .ToListAsync(cancellationToken);

        return new PatientMedicalHistoryResponse(
            patientId,
            conditions,
            allergies,
            medications);
    }

    public async Task<PatientMedicalHistoryResult> AddConditionAsync(
        CreatePatientConditionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out var clinicId))
            return Unauthenticated();

        var patientExists = await dbContext.Patients.AsNoTracking()
            .AnyAsync(patient => patient.Id == request.PatientId, cancellationToken);
        if (!patientExists)
            return PatientNotFound();

        var normalizedName = request.Name.Trim();
        var duplicate = await dbContext.PatientConditions.AsNoTracking()
            .AnyAsync(
                item =>
                    item.PatientId == request.PatientId &&
                    item.Name.ToUpper() == normalizedName.ToUpper(),
                cancellationToken);
        if (duplicate)
            return Conflict("This condition is already recorded for the patient.");

        var now = DateTime.UtcNow;
        var entity = new PatientCondition
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            PatientId = request.PatientId,
            Name = normalizedName,
            Notes = Clean(request.Notes),
            RecordedAtUtc = now,
            RecordedByUserId = userId,
            CreatedDate = now,
            CreateByUserId = userId
        };

        dbContext.PatientConditions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "PatientCondition.Created",
            nameof(PatientCondition),
            entity.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["patientId"] = request.PatientId,
                ["name"] = entity.Name
            },
            cancellationToken);

        return Success(await GetAsync(request.PatientId, cancellationToken));
    }

    public async Task<PatientMedicalHistoryResult> DeleteConditionAsync(
        DeletePatientMedicalHistoryItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out _, out _))
            return Unauthenticated();

        var entity = await dbContext.PatientConditions
            .SingleOrDefaultAsync(item => item.Id == request.ItemId, cancellationToken);
        if (entity is null)
            return ItemNotFound();

        var patientId = entity.PatientId;
        var name = entity.Name;

        dbContext.PatientConditions.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "PatientCondition.Deleted",
            nameof(PatientCondition),
            request.ItemId.ToString(),
            new Dictionary<string, object?>
            {
                ["patientId"] = patientId,
                ["name"] = name
            },
            cancellationToken);

        return Success(await GetAsync(patientId, cancellationToken));
    }

    public async Task<PatientMedicalHistoryResult> AddAllergyAsync(
        CreatePatientAllergyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out var clinicId))
            return Unauthenticated();

        var patientExists = await dbContext.Patients.AsNoTracking()
            .AnyAsync(patient => patient.Id == request.PatientId, cancellationToken);
        if (!patientExists)
            return PatientNotFound();

        var normalizedName = request.Name.Trim();
        var duplicate = await dbContext.PatientAllergies.AsNoTracking()
            .AnyAsync(
                item =>
                    item.PatientId == request.PatientId &&
                    item.Name.ToUpper() == normalizedName.ToUpper(),
                cancellationToken);
        if (duplicate)
            return Conflict("This allergy is already recorded for the patient.");

        var now = DateTime.UtcNow;
        var entity = new PatientAllergy
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            PatientId = request.PatientId,
            Name = normalizedName,
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
            "PatientAllergy.Created",
            nameof(PatientAllergy),
            entity.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["patientId"] = request.PatientId,
                ["name"] = entity.Name
            },
            cancellationToken);

        return Success(await GetAsync(request.PatientId, cancellationToken));
    }

    public async Task<PatientMedicalHistoryResult> DeleteAllergyAsync(
        DeletePatientMedicalHistoryItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out _, out _))
            return Unauthenticated();

        var entity = await dbContext.PatientAllergies
            .SingleOrDefaultAsync(item => item.Id == request.ItemId, cancellationToken);
        if (entity is null)
            return ItemNotFound();

        var patientId = entity.PatientId;
        var name = entity.Name;

        dbContext.PatientAllergies.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "PatientAllergy.Deleted",
            nameof(PatientAllergy),
            request.ItemId.ToString(),
            new Dictionary<string, object?>
            {
                ["patientId"] = patientId,
                ["name"] = name
            },
            cancellationToken);

        return Success(await GetAsync(patientId, cancellationToken));
    }

    public async Task<PatientMedicalHistoryResult> AddMedicationAsync(
        CreatePatientMedicationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out var clinicId))
            return Unauthenticated();

        var patientExists = await dbContext.Patients.AsNoTracking()
            .AnyAsync(patient => patient.Id == request.PatientId, cancellationToken);
        if (!patientExists)
            return PatientNotFound();

        var normalizedName = request.Name.Trim();
        var duplicate = await dbContext.PatientMedications.AsNoTracking()
            .AnyAsync(
                item =>
                    item.PatientId == request.PatientId &&
                    item.Name.ToUpper() == normalizedName.ToUpper(),
                cancellationToken);
        if (duplicate)
            return Conflict("This medication is already recorded for the patient.");

        var now = DateTime.UtcNow;
        var entity = new PatientMedication
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            PatientId = request.PatientId,
            Name = normalizedName,
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
            "PatientMedication.Created",
            nameof(PatientMedication),
            entity.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["patientId"] = request.PatientId,
                ["name"] = entity.Name
            },
            cancellationToken);

        return Success(await GetAsync(request.PatientId, cancellationToken));
    }

    public async Task<PatientMedicalHistoryResult> DeleteMedicationAsync(
        DeletePatientMedicalHistoryItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out _, out _))
            return Unauthenticated();

        var entity = await dbContext.PatientMedications
            .SingleOrDefaultAsync(item => item.Id == request.ItemId, cancellationToken);
        if (entity is null)
            return ItemNotFound();

        var patientId = entity.PatientId;
        var name = entity.Name;

        dbContext.PatientMedications.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "PatientMedication.Deleted",
            nameof(PatientMedication),
            request.ItemId.ToString(),
            new Dictionary<string, object?>
            {
                ["patientId"] = patientId,
                ["name"] = name
            },
            cancellationToken);

        return Success(await GetAsync(patientId, cancellationToken));
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

    private static PatientMedicalHistoryResult Success(PatientMedicalHistoryResponse? history) =>
        new(PatientMedicalHistoryOutcome.Success, history);

    private static PatientMedicalHistoryResult PatientNotFound() =>
        new(PatientMedicalHistoryOutcome.PatientNotFound);

    private static PatientMedicalHistoryResult ItemNotFound() =>
        new(PatientMedicalHistoryOutcome.ItemNotFound);

    private static PatientMedicalHistoryResult Conflict(string error) =>
        new(PatientMedicalHistoryOutcome.Conflict, Error: error);

    private static PatientMedicalHistoryResult Unauthenticated() =>
        new(PatientMedicalHistoryOutcome.Unauthenticated);
}
