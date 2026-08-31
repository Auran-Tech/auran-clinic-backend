using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Codes;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.IntegrationTests;

public sealed class SqlFoundationInvariantTests
{
    [SqlIntegrationFact]
    public async Task ClinicQueryFilterAndWriteGuard_BlockCrossTenantAccess()
    {
        var marker = Guid.NewGuid().ToString("N");
        var clinicAId = Guid.NewGuid();
        var clinicBId = Guid.NewGuid();
        var platformActor = TestActor.Platform(Guid.NewGuid());

        await using (var setup = CreateContext(platformActor))
        {
            setup.Clinics.AddRange(
                new Auran.Clinic.Domain.Entities.Clinic
                {
                    Id = clinicAId,
                    Name = $"Clinic A {marker}",
                    Code = $"TA-{marker}",
                    IsActive = true
                },
                new Auran.Clinic.Domain.Entities.Clinic
                {
                    Id = clinicBId,
                    Name = $"Clinic B {marker}",
                    Code = $"TB-{marker}",
                    IsActive = true
                });

            setup.Patients.AddRange(
                new Patient
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinicAId,
                    PatientNumber = $"A-{marker}",
                    FullName = $"Patient A {marker}",
                    Phone = $"A{marker}"
                },
                new Patient
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinicBId,
                    PatientNumber = $"B-{marker}",
                    FullName = $"Patient B {marker}",
                    Phone = $"B{marker}"
                });

            await setup.SaveChangesAsync();
        }

        var clinicAActor = TestActor.Clinic(Guid.NewGuid(), clinicAId);
        await using var tenantContext = CreateContext(clinicAActor);

        var visiblePatients = await tenantContext.Patients.AsNoTracking()
            .Where(x => x.FullName.Contains(marker))
            .ToListAsync();

        var visiblePatient = Assert.Single(visiblePatients);
        Assert.Equal(clinicAId, visiblePatient.ClinicId);
        Assert.StartsWith("Patient A", visiblePatient.FullName, StringComparison.Ordinal);

        tenantContext.Patients.Add(new Patient
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicBId,
            PatientNumber = $"SPOOF-{marker}",
            FullName = $"Spoofed Patient {marker}",
            Phone = $"S{marker}"
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => tenantContext.SaveChangesAsync());
        Assert.Contains("Cross-clinic write", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [SqlIntegrationFact]
    public async Task CodeGenerator_ProducesUniqueSequentialCodesUnderConcurrency()
    {
        var prefix = $"CI{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        var actor = TestActor.Platform(Guid.NewGuid());

        var tasks = Enumerable.Range(0, 20).Select(async _ =>
        {
            await using var context = CreateContext(actor);
            var service = new CodeGeneratorService(context, actor);
            return await service.GenerateAsync(CodeScope.Platform, null, CodeType.Clinic, prefix);
        });

        var codes = await Task.WhenAll(tasks);
        Assert.Equal(20, codes.Distinct(StringComparer.Ordinal).Count());

        var numbers = codes
            .Select(code => long.Parse(code.Split('-')[^1]))
            .OrderBy(number => number)
            .ToArray();

        Assert.Equal(Enumerable.Range(1, 20).Select(x => (long)x), numbers);
    }

    private static AuranClinicDbContext CreateContext(ICurrentActor actor)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings__DefaultConnection is required for SQL integration tests.");

        var options = new DbContextOptionsBuilder<AuranClinicDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new AuranClinicDbContext(options, actor);
    }

    private sealed class TestActor : ICurrentActor
    {
        private TestActor(
            ActorType actorType,
            Guid? platformUserId,
            Guid? clinicUserId,
            Guid? clinicId)
        {
            ActorType = actorType;
            PlatformUserId = platformUserId;
            ClinicUserId = clinicUserId;
            ClinicId = clinicId;
        }

        public bool IsAuthenticated => true;
        public ActorType ActorType { get; }
        public string? IdentityUserId => null;
        public Guid? PlatformUserId { get; }
        public Guid? ClinicUserId { get; }
        public Guid? ClinicId { get; }
        public bool IsClinicSuperUser => false;
        public string? DisplayName => "SQL Test Actor";
        public string? Email => "sql-test@example.com";

        public static TestActor Platform(Guid userId) =>
            new(ActorType.Platform, userId, null, null);

        public static TestActor Clinic(Guid userId, Guid clinicId) =>
            new(ActorType.Clinic, null, userId, clinicId);
    }
}
