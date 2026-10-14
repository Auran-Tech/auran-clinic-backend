using System.Data;
using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.DocumentationPolicies;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.DocumentationPolicies;

public sealed class DocumentationPolicyService(
    AuranClinicDbContext db,
    ICurrentUserContext actor,
    IAuditService audit) : IDocumentationPolicyService
{
    public static readonly HashSet<string> Fields = new(StringComparer.Ordinal)
    {
        "ChiefComplaint", "Examination", "Diagnosis", "Notes", "TreatmentPlan"
    };

    public static string NormalizeSpecialty(string value) => value.Trim().ToUpperInvariant();

    public static bool IsValidSpecialty(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 64 &&
        value.All(c => char.IsAsciiLetterUpper(c) || char.IsDigit(c) || c is '-' or '_');

    public async Task<DocumentationPolicyResponse?> GetAsync(string specialtyCode, CancellationToken ct = default)
    {
        if (actor.ClinicId is not Guid clinicId || !IsValidSpecialty(specialtyCode))
            return null;
        var fields = await db.Database.SqlQuery<string>(
            $"SELECT RequiredFields AS Value FROM dbo.ClinicDocumentationSpecialtyPolicies WHERE ClinicId = {clinicId} AND SpecialtyCode = {specialtyCode}")
            .SingleOrDefaultAsync(ct);
        if (fields is null) return null;
        var version = await db.Database.SqlQuery<int>(
            $"SELECT Version AS Value FROM dbo.ClinicDocumentationSpecialtyPolicies WHERE ClinicId = {clinicId} AND SpecialtyCode = {specialtyCode}")
            .SingleAsync(ct);
        return new DocumentationPolicyResponse(specialtyCode, fields.Split(','), version);
    }

    public async Task<DocumentationPolicyResponse> UpdateAsync(string specialtyCode, string[] fields, CancellationToken ct = default)
    {
        if (actor.ClinicId is not Guid clinicId || actor.UserId is not Guid userId)
            throw new InvalidOperationException("Clinic identity required.");
        if (!IsValidSpecialty(specialtyCode) || fields.Length == 0 ||
            fields.Length > Fields.Count || fields.Distinct(StringComparer.Ordinal).Count() != fields.Length ||
            fields.Any(field => !Fields.Contains(field)))
            throw new ArgumentException("Invalid specialty or required clinical fields.");

        var serialized = string.Join(",", fields);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var lockResult = new SqlParameter("@lockResult", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var resource = new SqlParameter("@resource", SqlDbType.NVarChar, 255)
        {
            Value = $"auran:documentation-policy:{clinicId:N}:{specialtyCode}"
        };
        await db.Database.ExecuteSqlRawAsync(
            "EXEC @lockResult = sys.sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000",
            [lockResult, resource], ct);
        if (lockResult.Value is not int lockCode || lockCode < 0)
            throw new InvalidOperationException("Unable to acquire documentation policy update lock.");

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE dbo.ClinicDocumentationSpecialtyPolicies
            SET RequiredFields = {serialized}, Version = Version + 1,
                UpdatedAtUtc = SYSUTCDATETIME(), UpdatedByUserId = {userId}
            WHERE ClinicId = {clinicId} AND SpecialtyCode = {specialtyCode};
            IF @@ROWCOUNT = 0
                INSERT INTO dbo.ClinicDocumentationSpecialtyPolicies
                    (ClinicId, SpecialtyCode, RequiredFields, Version, UpdatedByUserId)
                VALUES ({clinicId}, {specialtyCode}, {serialized}, 1, {userId});
            """, ct);

        var result = await GetAsync(specialtyCode, ct)
            ?? throw new InvalidOperationException("Policy persistence failed.");

        await audit.WriteAsync(
            "ClinicalDocumentation.PolicyUpdated", "ClinicDocumentationSpecialtyPolicy",
            specialtyCode,
            new Dictionary<string, object?>
            {
                ["clinicId"] = clinicId, ["specialtyCode"] = specialtyCode,
                ["policyVersion"] = result.Version, ["requiredFields"] = fields
            }, ct);
        await tx.CommitAsync(ct);
        return result;
    }

    public async Task<bool> AssignDoctorAsync(Guid doctorId, string specialtyCode, CancellationToken ct = default)
    {
        if (actor.ClinicId is not Guid clinicId || actor.UserId is not Guid userId ||
            !IsValidSpecialty(specialtyCode))
            return false;

        var doctorExists = await db.Users.AsNoTracking()
            .AnyAsync(user => user.Id == doctorId && user.ClinicId == clinicId && user.IsActive, ct);
        if (!doctorExists)
            return false;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE dbo.DoctorDocumentationSpecialties
            SET SpecialtyCode = {specialtyCode}, UpdatedAtUtc = SYSUTCDATETIME()
            WHERE ClinicId = {clinicId} AND DoctorId = {doctorId};
            IF @@ROWCOUNT = 0
                INSERT INTO dbo.DoctorDocumentationSpecialties (ClinicId, DoctorId, SpecialtyCode)
                VALUES ({clinicId}, {doctorId}, {specialtyCode});
            """, ct);
        await audit.WriteAsync(
            "ClinicalDocumentation.DoctorSpecialtyAssigned", "DoctorDocumentationSpecialty",
            doctorId.ToString(),
            new Dictionary<string, object?>
            {
                ["clinicId"] = clinicId, ["doctorId"] = doctorId,
                ["specialtyCode"] = specialtyCode, ["actorId"] = userId
            }, ct);
        await tx.CommitAsync(ct);
        return true;
    }
}
