using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.UnitTests;

public sealed class TenantPersistenceBoundaryTests
{
    [Fact]
    public async Task AuthenticatedClinicQuery_ReturnsOnlyCurrentClinicEntities()
    {
        var databaseName = Guid.NewGuid().ToString();
        var clinicA = Guid.NewGuid();
        var clinicB = Guid.NewGuid();

        await using (var seedContext = CreateContext(databaseName, TestCurrentUserContext.Unauthenticated()))
        {
            seedContext.Patients.AddRange(
                CreatePatient(clinicA, "A"),
                CreatePatient(clinicB, "B"));
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(databaseName, TestCurrentUserContext.ForClinic(clinicA));
        var patients = await context.Patients.AsNoTracking().ToListAsync();

        var patient = Assert.Single(patients);
        Assert.Equal(clinicA, patient.ClinicId);
    }

    [Fact]
    public async Task UnauthenticatedQuery_RemainsUnfilteredForAuthenticationAndBootstrapFlows()
    {
        var databaseName = Guid.NewGuid().ToString();
        var clinicA = Guid.NewGuid();
        var clinicB = Guid.NewGuid();

        await using var context = CreateContext(databaseName, TestCurrentUserContext.Unauthenticated());
        context.Patients.AddRange(
            CreatePatient(clinicA, "A"),
            CreatePatient(clinicB, "B"));
        await context.SaveChangesAsync();

        var patients = await context.Patients.AsNoTracking().ToListAsync();

        Assert.Equal(2, patients.Count);
    }

    [Fact]
    public async Task AuthenticatedClinicInsert_AssignsCurrentClinicWhenClinicIdIsEmpty()
    {
        var clinicId = Guid.NewGuid();
        await using var context = CreateContext(
            Guid.NewGuid().ToString(),
            TestCurrentUserContext.ForClinic(clinicId));

        var patient = CreatePatient(Guid.Empty, "A");
        context.Patients.Add(patient);

        await context.SaveChangesAsync();

        Assert.Equal(clinicId, patient.ClinicId);
    }

    [Fact]
    public async Task AuthenticatedClinicInsert_RejectsDifferentClinicId()
    {
        var currentClinicId = Guid.NewGuid();
        await using var context = CreateContext(
            Guid.NewGuid().ToString(),
            TestCurrentUserContext.ForClinic(currentClinicId));

        context.Patients.Add(CreatePatient(Guid.NewGuid(), "B"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());

        Assert.Equal("Cross-clinic write access is not allowed.", exception.Message);
    }

    [Theory]
    [InlineData(EntityState.Modified)]
    [InlineData(EntityState.Deleted)]
    public async Task AttachedCrossClinicEntity_RejectsUpdateOrDelete(EntityState state)
    {
        var currentClinicId = Guid.NewGuid();
        var otherClinicId = Guid.NewGuid();
        await using var context = CreateContext(
            Guid.NewGuid().ToString(),
            TestCurrentUserContext.ForClinic(currentClinicId));

        var patient = CreatePatient(otherClinicId, "B");
        patient.Id = Guid.NewGuid();
        context.Attach(patient);
        context.Entry(patient).State = state;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());

        Assert.Equal("Cross-clinic write access is not allowed.", exception.Message);
    }

    private static AuranClinicDbContext CreateContext(
        string databaseName,
        ICurrentUserContext currentUserContext)
    {
        var options = new DbContextOptionsBuilder<AuranClinicDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new AuranClinicDbContext(options, currentUserContext);
    }

    private static Patient CreatePatient(Guid clinicId, string suffix) => new()
    {
        ClinicId = clinicId,
        PatientNumber = $"AU-{suffix}",
        FullName = $"Patient {suffix}",
        Phone = $"0100000000{suffix}"
    };

    private sealed class TestCurrentUserContext : ICurrentUserContext
    {
        public bool IsAuthenticated { get; init; }
        public Guid? UserId { get; init; }
        public Guid? ClinicId { get; init; }
        public bool IsSuperUser { get; init; }

        public static TestCurrentUserContext Unauthenticated() => new();

        public static TestCurrentUserContext ForClinic(Guid clinicId) => new()
        {
            IsAuthenticated = true,
            UserId = Guid.NewGuid(),
            ClinicId = clinicId
        };
    }
}
