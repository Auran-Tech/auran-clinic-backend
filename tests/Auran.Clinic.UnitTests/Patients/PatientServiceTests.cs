using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Codes;
using Auran.Clinic.Application.Patients;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Patients;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.UnitTests.Patients;

public sealed class PatientServiceTests
{
    [Fact]
    public async Task ListAsync_returns_only_patients_from_current_clinic()
    {
        var databaseName = Guid.NewGuid().ToString();
        var clinicA = Guid.NewGuid();
        var clinicB = Guid.NewGuid();

        await SeedAsync(databaseName, clinicA, clinicB);

        await using var db = CreateDb(databaseName, new TestCurrentUserContext(clinicA));
        var service = CreateService(db, clinicA);

        var result = await service.ListAsync(new PatientQuery { Page = 1, PageSize = 20 });

        var patient = Assert.Single(result.Data);
        Assert.Equal("PAT-A", patient.PatientNumber);
        Assert.Equal(clinicA, db.Patients.Single().ClinicId);
    }

    [Fact]
    public async Task FindDuplicatesAsync_matches_normalized_phone_within_current_clinic()
    {
        var databaseName = Guid.NewGuid().ToString();
        var clinicA = Guid.NewGuid();
        var clinicB = Guid.NewGuid();

        await SeedAsync(databaseName, clinicA, clinicB);

        await using var db = CreateDb(databaseName, new TestCurrentUserContext(clinicA));
        var service = CreateService(db, clinicA);

        var result = await service.FindDuplicatesAsync(new CreatePatientRequest
        {
            FullName = "Different Name",
            Phone = "+20 100 123 4567"
        });

        var duplicate = Assert.Single(result);
        Assert.Equal("PAT-A", duplicate.PatientNumber);
        Assert.Equal("phone", duplicate.MatchReason);
    }

    [Fact]
    public async Task GetAsync_treats_cross_clinic_patient_as_not_found()
    {
        var databaseName = Guid.NewGuid().ToString();
        var clinicA = Guid.NewGuid();
        var clinicB = Guid.NewGuid();
        var (_, clinicBPatientId) = await SeedAsync(databaseName, clinicA, clinicB);

        await using var db = CreateDb(databaseName, new TestCurrentUserContext(clinicA));
        var service = CreateService(db, clinicA);

        var result = await service.GetAsync(clinicBPatientId);

        Assert.Null(result);
    }

    private static PatientService CreateService(AuranClinicDbContext db, Guid clinicId) =>
        new(
            db,
            new TestCurrentUserContext(clinicId),
            new TestCodeGenerator(),
            new TestAuditService());

    private static AuranClinicDbContext CreateDb(string name, ICurrentUserContext currentUser)
    {
        var options = new DbContextOptionsBuilder<AuranClinicDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AuranClinicDbContext(options, currentUser);
    }

    private static async Task<(Guid ClinicAPatientId, Guid ClinicBPatientId)> SeedAsync(
        string databaseName,
        Guid clinicA,
        Guid clinicB)
    {
        await using var db = CreateDb(databaseName, new AnonymousCurrentUserContext());

        var patientAId = Guid.NewGuid();
        var patientBId = Guid.NewGuid();

        db.Patients.AddRange(
            new Patient
            {
                Id = patientAId,
                ClinicId = clinicA,
                PatientNumber = "PAT-A",
                FullName = "Ahmed Ali",
                Phone = "+201001234567",
                CreatedDate = DateTime.UtcNow
            },
            new Patient
            {
                Id = patientBId,
                ClinicId = clinicB,
                PatientNumber = "PAT-B",
                FullName = "Other Clinic",
                Phone = "+201112345678",
                CreatedDate = DateTime.UtcNow
            });

        await db.SaveChangesAsync();
        return (patientAId, patientBId);
    }

    private sealed class TestCurrentUserContext(Guid clinicId) : ICurrentUserContext
    {
        public bool IsAuthenticated => true;
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? ClinicId => clinicId;
        public bool IsSuperUser => false;
    }

    private sealed class AnonymousCurrentUserContext : ICurrentUserContext
    {
        public bool IsAuthenticated => false;
        public Guid? UserId => null;
        public Guid? ClinicId => null;
        public bool IsSuperUser => false;
    }

    private sealed class TestCodeGenerator : ICodeGeneratorService
    {
        public Task<string> GenerateAsync(
            CodeScope scope,
            Guid? clinicId,
            CodeType codeType,
            string prefix,
            CancellationToken cancellationToken = default) =>
            Task.FromResult("PAT-2026-1");
    }

    private sealed class TestAuditService : IAuditService
    {
        public Task WriteAsync(
            string action,
            string entityType,
            string? entityId = null,
            IReadOnlyDictionary<string, object?>? metadata = null,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<AuditLogResponse>> GetRecentAsync(
            int take = 100,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditLogResponse>>(Array.Empty<AuditLogResponse>());
    }
}
