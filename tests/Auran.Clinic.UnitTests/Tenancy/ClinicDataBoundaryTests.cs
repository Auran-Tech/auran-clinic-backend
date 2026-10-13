using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.UnitTests.Tenancy;

public sealed class ClinicDataBoundaryTests
{
    [Fact]
    public async Task Authenticated_clinic_reads_only_its_own_patients()
    {
        var clinicA = Guid.NewGuid();
        var clinicB = Guid.NewGuid();
        var options = Options();

        await using (var seed = new AuranClinicDbContext(options, new TestActor(false, null)))
        {
            seed.Patients.AddRange(Patient(clinicA), Patient(clinicB));
            await seed.SaveChangesAsync();
        }

        await using var scoped = new AuranClinicDbContext(options, new TestActor(true, clinicA));
        var patients = await scoped.Patients.AsNoTracking().ToListAsync();

        Assert.Single(patients);
        Assert.Equal(clinicA, patients[0].ClinicId);
    }

    [Fact]
    public async Task Authenticated_actor_without_clinic_scope_cannot_read_clinic_data()
    {
        var options = Options();
        await using (var seed = new AuranClinicDbContext(options, new TestActor(false, null)))
        {
            seed.Patients.Add(Patient(Guid.NewGuid()));
            await seed.SaveChangesAsync();
        }

        await using var scoped = new AuranClinicDbContext(options, new TestActor(true, null));
        Assert.Empty(await scoped.Patients.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Cross_clinic_writes_are_rejected()
    {
        var clinicA = Guid.NewGuid();
        var clinicB = Guid.NewGuid();
        await using var scoped = new AuranClinicDbContext(Options(), new TestActor(true, clinicA));
        scoped.Patients.Add(Patient(clinicB));

        await Assert.ThrowsAsync<InvalidOperationException>(() => scoped.SaveChangesAsync());
    }

    [Fact]
    public async Task Authenticated_actor_without_scope_cannot_write_clinic_data()
    {
        await using var scoped = new AuranClinicDbContext(Options(), new TestActor(true, null));
        scoped.Patients.Add(Patient(Guid.NewGuid()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => scoped.SaveChangesAsync());
    }

    private static DbContextOptions<AuranClinicDbContext> Options() =>
        new DbContextOptionsBuilder<AuranClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static Patient Patient(Guid clinicId) => new()
    {
        Id = Guid.NewGuid(),
        ClinicId = clinicId,
        PatientNumber = Guid.NewGuid().ToString("N"),
        FullName = "Isolation Test",
        Phone = "01000000000"
    };

    private sealed record TestActor(bool IsAuthenticated, Guid? ClinicId) : ICurrentUserContext
    {
        public Guid? UserId => IsAuthenticated ? Guid.NewGuid() : null;
        public bool IsSuperUser => false;
    }
}
