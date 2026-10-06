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
        PatientQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var patients = dbContext.Patients.AsNoTracking();

        var search = Clean(query.Search);
        if (search is not null)
        {
            patients = patients.Where(patient =>
                patient.FullName.Contains(search) ||
                patient.Phone.Contains(search) ||
                patient.PatientNumber.Contains(search));
        }

        var totalCount = await patients.CountAsync(cancellationToken);
        var data = await patients
            .OrderBy(patient => patient.FullName)
            .ThenBy(patient => patient.PatientNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(patient => new PatientResponse(
                patient.Id,
                patient.PatientNumber,
                patient.FullName,
                patient.Phone,
                patient.Gender,
                patient.DateOfBirth,
                patient.Notes,
                patient.CreatedDate,
                patient.UpdatedDate))
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<PatientResponse>
        {
            Data = data,
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
        return await dbContext.Patients.AsNoTracking()
            .Where(patient => patient.Id == patientId)
            .Select(patient => new PatientResponse(
                patient.Id,
                patient.PatientNumber,
                patient.FullName,
                patient.Phone,
                patient.Gender,
                patient.DateOfBirth,
                patient.Notes,
                patient.CreatedDate,
                patient.UpdatedDate))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PatientDuplicateCandidateResponse>> FindDuplicatesAsync(
        CreatePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        var phone = NormalizePhone(request.Phone);
        var fullName = NormalizeName(request.FullName);
        var dateOfBirth = request.DateOfBirth;

        var candidates = await dbContext.Patients.AsNoTracking()
            .Where(patient =>
                patient.Phone == phone ||
                (dateOfBirth != null &&
                 patient.DateOfBirth == dateOfBirth &&
                 patient.FullName.ToUpper() == fullName.ToUpper()))
            .OrderBy(patient => patient.FullName)
            .Take(10)
            .ToListAsync(cancellationToken);

        return candidates.Select(patient => new PatientDuplicateCandidateResponse(
            patient.Id,
            patient.PatientNumber,
            patient.FullName,
            patient.Phone,
            patient.DateOfBirth,
            patient.Phone == phone ? "phone" : "name_and_date_of_birth"))
            .ToList();
    }

    public async Task<PatientManagementResult> CreateAsync(
        CreatePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var userId, out var clinicId))
            return new PatientManagementResult(PatientManagementOutcome.Unauthenticated);

        var phone = NormalizePhone(request.Phone);
        var duplicatePhone = await dbContext.Patients.AsNoTracking()
            .AnyAsync(patient => patient.Phone == phone, cancellationToken);

        if (duplicatePhone)
        {
            return new PatientManagementResult(
                PatientManagementOutcome.Conflict,
                Error: "A patient with the same phone number already exists.");
        }

        var clinic = await dbContext.Clinics.AsNoTracking()
            .SingleAsync(item => item.Id == clinicId, cancellationToken);
        var prefix = string.IsNullOrWhiteSpace(clinic.PatientNumberPrefix)
            ? "PAT"
            : clinic.PatientNumberPrefix.Trim();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var patientNumber = await codeGeneratorService.GenerateAsync(
                CodeScope.Clinic,
                clinicId,
                CodeType.Patient,
                prefix,
                cancellationToken);

            var now = DateTime.UtcNow;
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                PatientNumber = patientNumber,
                FullName = NormalizeName(request.FullName),
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
                    ["patientNumber"] = patient.PatientNumber
                },
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new PatientManagementResult(
                PatientManagementOutcome.Success,
                Map(patient));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PatientManagementResult> UpdateAsync(
        UpdatePatientRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var userId, out _))
            return new PatientManagementResult(PatientManagementOutcome.Unauthenticated);

        var patient = await dbContext.Patients
            .SingleOrDefaultAsync(item => item.Id == request.PatientId, cancellationToken);
        if (patient is null)
            return new PatientManagementResult(PatientManagementOutcome.NotFound);

        var phone = NormalizePhone(request.Phone);
        var duplicatePhone = await dbContext.Patients.AsNoTracking()
            .AnyAsync(item => item.Id != patient.Id && item.Phone == phone, cancellationToken);
        if (duplicatePhone)
        {
            return new PatientManagementResult(
                PatientManagementOutcome.Conflict,
                Error: "A patient with the same phone number already exists.");
        }

        patient.FullName = NormalizeName(request.FullName);
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
            new Dictionary<string, object?>
            {
                ["patientNumber"] = patient.PatientNumber
            },
            cancellationToken);

        return new PatientManagementResult(
            PatientManagementOutcome.Success,
            Map(patient));
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

    private static PatientResponse Map(Patient patient) => new(
        patient.Id,
        patient.PatientNumber,
        patient.FullName,
        patient.Phone,
        patient.Gender,
        patient.DateOfBirth,
        patient.Notes,
        patient.CreatedDate,
        patient.UpdatedDate);

    private static string NormalizeName(string value) =>
        string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string NormalizePhone(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith('+'))
            return "+" + new string(trimmed.Skip(1).Where(char.IsDigit).ToArray());

        return new string(trimmed.Where(char.IsDigit).ToArray());
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
