using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DomainClinic = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.IntegrationTests;

public sealed class VisitPersistenceInvariantTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task QueueEntry_DatabaseConstraint_AllowsOnlyOneEntryPerVisit()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        var clinic = await CreateClinicAsync(dbContext, "queue-unique");
        var doctor = await CreateUserAsync(dbContext, userManager, clinic.Id, "queue-doctor");
        var patient = await CreatePatientAsync(dbContext, clinic.Id, "queue-patient");
        var visit = await CreateVisitAsync(dbContext, clinic.Id, patient.Id, doctor.Id);
        var status = await CreateWorkflowStatusAsync(dbContext, clinic.Id, "WAITING");

        var firstRows = await InsertQueueEntryAsync(dbContext, clinic.Id, patient.Id, visit.Id, doctor.Id, status.Id);
        Assert.Equal(1, firstRows);

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            InsertQueueEntryAsync(dbContext, clinic.Id, patient.Id, visit.Id, doctor.Id, status.Id));

        Assert.Contains(exception.Number, new[] { 2601, 2627 });
        Assert.Contains("IX_QueueEntries_ClinicId_VisitId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VisitSession_DatabaseConstraint_AllowsHistoryButOnlyOneActiveSession()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        var clinic = await CreateClinicAsync(dbContext, "session-unique");
        var doctor = await CreateUserAsync(dbContext, userManager, clinic.Id, "session-doctor");
        var patient = await CreatePatientAsync(dbContext, clinic.Id, "session-patient");
        var visit = await CreateVisitAsync(dbContext, clinic.Id, patient.Id, doctor.Id);

        var endedRows = await InsertVisitSessionAsync(
            dbContext, clinic.Id, visit.Id, doctor.Id, doctor.Id, endedAtUtc: DateTime.UtcNow);
        Assert.Equal(1, endedRows);

        var activeRows = await InsertVisitSessionAsync(
            dbContext, clinic.Id, visit.Id, doctor.Id, doctor.Id, endedAtUtc: null);
        Assert.Equal(1, activeRows);

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            InsertVisitSessionAsync(
                dbContext, clinic.Id, visit.Id, doctor.Id, doctor.Id, endedAtUtc: null));

        Assert.Contains(exception.Number, new[] { 2601, 2627 });
        Assert.Contains("IX_VisitSessions_ClinicId_VisitId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Visit_RowVersion_RejectsStaleConcurrentUpdate()
    {
        Guid visitId;
        using (var setupScope = factory.Services.CreateScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
            var userManager = setupScope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();
            var clinic = await CreateClinicAsync(dbContext, "visit-concurrency");
            var doctor = await CreateUserAsync(dbContext, userManager, clinic.Id, "concurrency-doctor");
            var patient = await CreatePatientAsync(dbContext, clinic.Id, "concurrency-patient");
            visitId = (await CreateVisitAsync(dbContext, clinic.Id, patient.Id, doctor.Id)).Id;
        }

        using var scopeA = factory.Services.CreateScope();
        using var scopeB = factory.Services.CreateScope();
        var contextA = scopeA.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var contextB = scopeB.ServiceProvider.GetRequiredService<AuranClinicDbContext>();

        var visitA = await contextA.Visits.SingleAsync(visit => visit.Id == visitId);
        var visitB = await contextB.Visits.SingleAsync(visit => visit.Id == visitId);
        Assert.NotEmpty(visitA.RowVersion);
        Assert.Equal(visitA.RowVersion, visitB.RowVersion);

        visitA.Notes = "First writer";
        await contextA.SaveChangesAsync();

        visitB.Notes = "Stale writer";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => contextB.SaveChangesAsync());
    }

    private static Task<int> InsertQueueEntryAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid patientId,
        Guid visitId,
        Guid doctorId,
        Guid workflowStatusId)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [QueueEntries]
                ([Id], [ClinicId], [PatientId], [VisitId], [DoctorId], [WorkflowStatusId], [EntryAtUtc], [CreatedDate])
            VALUES ({id}, {clinicId}, {patientId}, {visitId}, {doctorId}, {workflowStatusId}, {now}, {now})
            """);
    }

    private static Task<int> InsertVisitSessionAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid visitId,
        Guid doctorId,
        Guid createdByUserId,
        DateTime? endedAtUtc)
    {
        var id = Guid.NewGuid();
        var startedAtUtc = DateTime.UtcNow.AddMinutes(-5);
        var now = DateTime.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [VisitSessions]
                ([Id], [ClinicId], [VisitId], [DoctorId], [StartedAtUtc], [EndedAtUtc], [CreatedByUserId], [CreatedDate])
            VALUES ({id}, {clinicId}, {visitId}, {doctorId}, {startedAtUtc}, {endedAtUtc}, {createdByUserId}, {now})
            """);
    }

    private static async Task<DomainClinic> CreateClinicAsync(AuranClinicDbContext dbContext, string label)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var clinic = new DomainClinic
        {
            Id = Guid.NewGuid(),
            Name = $"Visit Invariant {label} {suffix}",
            Code = $"VI-{suffix}",
            CreatedDate = DateTime.UtcNow
        };
        dbContext.Clinics.Add(clinic);
        await dbContext.SaveChangesAsync();
        return clinic;
    }

    private static async Task<User> CreateUserAsync(
        AuranClinicDbContext dbContext,
        UserManager<ApplicationIdentityUser> userManager,
        Guid clinicId,
        string label)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var email = $"{label}-{suffix}@auran.local";
        var identityUser = new ApplicationIdentityUser
        {
            UserName = email,
            Email = email,
            LockoutEnabled = true
        };

        var identityResult = await userManager.CreateAsync(identityUser, "ValidPassword1");
        Assert.True(identityResult.Succeeded, string.Join(", ", identityResult.Errors.Select(error => error.Description)));

        var user = new User
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            IdentityUserId = identityUser.Id,
            FullName = $"Visit Invariant {label}",
            Email = email,
            CreatedDate = DateTime.UtcNow
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private static async Task<Patient> CreatePatientAsync(AuranClinicDbContext dbContext, Guid clinicId, string label)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            PatientNumber = $"VI-{suffix}",
            FullName = $"Visit Invariant {label}",
            Phone = $"+20{Math.Abs(Guid.NewGuid().GetHashCode()):D10}"[..13],
            CreatedDate = DateTime.UtcNow
        };
        dbContext.Patients.Add(patient);
        await dbContext.SaveChangesAsync();
        return patient;
    }

    private static async Task<Visit> CreateVisitAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid patientId,
        Guid doctorId)
    {
        var visit = new Visit
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            PatientId = patientId,
            DoctorId = doctorId,
            Status = VisitStatus.Open,
            DocumentationStatus = DocumentationStatus.NotStarted,
            EntryAtUtc = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        dbContext.Visits.Add(visit);
        await dbContext.SaveChangesAsync();
        return visit;
    }

    private static async Task<WorkflowStatus> CreateWorkflowStatusAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        string code)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var status = new WorkflowStatus
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            Code = $"{code}-{suffix}",
            Name = $"Status {code} {suffix}",
            Color = "#000000",
            CreatedDate = DateTime.UtcNow
        };
        dbContext.WorkflowStatuses.Add(status);
        await dbContext.SaveChangesAsync();
        return status;
    }
}
