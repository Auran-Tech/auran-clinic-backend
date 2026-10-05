using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Codes;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Patients;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Patients;

public sealed class PatientService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    ICodeGeneratorService codeGeneratorService,
    IAuditService auditService) : IPatientService
{
    public async Task<PaginatedResponse<PatientResponse>> ListAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<Patient> query = dbContext.Patients.AsNoTracking();

        var normalizedSearch = Clean(search);
        if (normalizedSearch is not null)
        {
            query = query.Where(patient =>
                patient.PatientNumber.Contains(normalizedSearch) ||
                patient.FullName.Contains(normalizedSearch) ||
                patient.Phone.Contains(normalizedSearch));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var patients = await query
            .OrderBy(patient => patient.FullName)
            .ThenBy(patient => patient.PatientNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(patient => Map(patient))
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<PatientResponse>
        {
            Data = patients,
            Setting = new PaginationInfo
            {
                TotalCount = totalCount,
                RowCount = pageSize,
                CurrentPage = page
            }
        };
    }

    public async Task<PatientResponse?> GetAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Patients
            .AsNoTracking()
            .Where(patient => patient.Id == patientId)
            .Select(patient => Map(patient))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PatientDuplicateCheckResponse> CheckDuplicatesAsync(
        PatientDuplicateCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        var phone = NormalizePhone(request.Phone);
        var fullName = request.FullName.Trim();

        IQueryable<Patient> query = dbContext.Patients.AsNoTracking();

        if (request.ExcludePatientId.HasValue)
            query = query.Where(patient => patient.Id != request.ExcludePatientId.Value);

        query = request.DateOfBirth.HasValue
            ? query.Where(patient =>
                patient.Phone == phone ||
                (patient.FullName == fullName && patient.DateOfBirth == request.DateOfBirth))
            : query.Where(patient => patient.Phone == phone);

        var matches = await query
            .OrderBy(patient => patient.FullName)
            .Take(10)
            .ToListAsync(cancellationToken);

        var candidates = matches.Select(patient =>
        {
            var reasons = new List<string>();
            if (patient.Phone == phone)
                reasons.Add("phone");
            if (request.DateOfBirth.HasValue &&
                patient.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase) &&
                patient.DateOfBirth == request.DateOfBirth)
            {
                reasons.Add("name_and_date_of_birth");
            }

            return new PatientDuplicateCandidate(
                patient.Id,
                patient.PatientNumber,
                patient.FullName,
                patient.Phone,
                patient.DateOfBirth,
                reasons);
        }).ToList();

        return new PatientDuplicateCheckResponse(candidates.Count > 0, candidates);
    }

    public async Task<PatientMutationResult> CreateAsync(
        CreatePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var userId, out var clinicId))
            return new PatientMutationResult(PatientMutationOutcome.Unauthenticated);

        var fullName = request.FullName.Trim();
        var phone = NormalizePhone(request.Phone);

        var existingPhone = await dbContext.Patients
            .AsNoTracking()
            .AnyAsync(patient => patient.Phone == phone, cancellationToken);
        if (existingPhone)
        {
            return new PatientMutationResult(
                PatientMutationOutcome.Conflict,
                Error: "A patient with this phone number already exists.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var patientNumber = await codeGeneratorService.GenerateAsync(
                CodeScope.Clinic,
                clinicId,
                CodeType.Patient,
                "P",
                cancellationToken);

            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                PatientNumber = patientNumber,
                FullName = fullName,
                Phone = phone,
                Gender = Clean(request.Gender),
                DateOfBirth = request.DateOfBirth,
                Notes = Clean(request.Notes),
                CreatedDate = now,
                CreateByUserId = userId
            };

            dbContext.Patients.Add(patient);
            await dbContext.SaveChangesAsync(cancellationToken);

            await auditService.WriteAsync(
                "Patient.Created",
                nameof(Patient),
                patient.Id.ToString(),
                new Dictionary<string, object?>
                {
                    ["patientNumber"] = patient.PatientNumber,
                    ["phone"] = patient.Phone
                },
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new PatientMutationResult(PatientMutationOutcome.Success, Map(patient));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PatientMutationResult> UpdateAsync(
        Guid patientId,
        UpdatePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var userId, out _))
            return new PatientMutationResult(PatientMutationOutcome.Unauthenticated);

        var patient = await dbContext.Patients
            .SingleOrDefaultAsync(item => item.Id == patientId, cancellationToken);
        if (patient is null)
            return new PatientMutationResult(PatientMutationOutcome.NotFound);

        var phone = NormalizePhone(request.Phone);
        var phoneConflict = await dbContext.Patients
            .AsNoTracking()
            .AnyAsync(item => item.Id != patientId && item.Phone == phone, cancellationToken);
        if (phoneConflict)
        {
            return new PatientMutationResult(
                PatientMutationOutcome.Conflict,
                Error: "A patient with this phone number already exists.");
        }

        patient.FullName = request.FullName.Trim();
        patient.Phone = phone;
        patient.Gender = Clean(request.Gender);
        patient.DateOfBirth = request.DateOfBirth;
        patient.Notes = Clean(request.Notes);
        patient.UpdatedDate = DateTime.UtcNow;
        patient.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(
            "Patient.Updated",
            nameof(Patient),
            patient.Id.ToString(),
            new Dictionary<string, object?> { ["patientNumber"] = patient.PatientNumber },
            cancellationToken);

        return new PatientMutationResult(PatientMutationOutcome.Success, Map(patient));
    }

    private bool TryGetCurrentActor(out Guid userId, out Guid clinicId)
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

    private static PatientResponse Map(Patient patient) =>
        new(
            patient.Id,
            patient.PatientNumber,
            patient.FullName,
            patient.Phone,
            patient.Gender,
            patient.DateOfBirth,
            patient.Notes,
            patient.CreatedDate,
            patient.UpdatedDate);

    private static string NormalizePhone(string value)
    {
        var trimmed = value.Trim();
        var prefix = trimmed.StartsWith('+') ? "+" : string.Empty;
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        return prefix + digits;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
