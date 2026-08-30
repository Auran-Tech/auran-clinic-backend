using System.Security.Claims;
using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Auran.Clinic.IntegrationTests;

public sealed class PersistenceFoundationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task TenantQueryFilter_ShouldReturnOnlyCurrentClinicPatients()
    {
        await using var scope = CreateClinicScope(ApiFactory.ClinicAId, ApiFactory.SuperUserId);
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();

        var patients = await dbContext.Patients.AsNoTracking().ToListAsync();

        var patient = Assert.Single(patients);
        Assert.Equal(ApiFactory.ClinicAId, patient.ClinicId);
        Assert.Equal("Clinic A Patient", patient.FullName);
    }

    [Fact]
    public async Task SaveChanges_ShouldRejectCrossClinicMutation()
    {
        await using var scope = CreateClinicScope(ApiFactory.ClinicAId, ApiFactory.SuperUserId);
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        dbContext.Patients.Add(new Patient
        {
            ClinicId = ApiFactory.ClinicBId,
            PatientNumber = "INVALID-CROSS-TENANT",
            FullName = "Cross Tenant Patient",
            Phone = "01000000003"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task CodeGenerator_ShouldIncrementWithinClinicAndScope()
    {
        await using var scope = CreateClinicScope(ApiFactory.ClinicAId, ApiFactory.SuperUserId);
        var generator = scope.ServiceProvider.GetRequiredService<ICodeGenerator>();

        var first = await generator.GetNextNumberAsync("PatientNumber", "2026");
        var second = await generator.GetNextNumberAsync("PatientNumber", "2026");
        var nextYear = await generator.GetNextNumberAsync("PatientNumber", "2027");

        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Equal(1, nextYear);
    }

    [Fact]
    public async Task QueueEntry_ShouldBeUniquePerVisit()
    {
        await using var scope = CreateClinicScope(ApiFactory.ClinicAId, ApiFactory.SuperUserId);
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var patient = await dbContext.Patients.SingleAsync();
        var status = new WorkflowStatus
        {
            Code = "WAITING-" + Guid.NewGuid().ToString("N"),
            Name = "Waiting",
            Color = "#316DF4",
            SortOrder = 1
        };
        var visit = new Visit
        {
            PatientId = patient.Id,
            DoctorId = ApiFactory.SuperUserId,
            EntryAtUtc = DateTime.UtcNow
        };
        dbContext.AddRange(status, visit);
        await dbContext.SaveChangesAsync();

        dbContext.QueueEntries.Add(new QueueEntry
        {
            PatientId = patient.Id,
            VisitId = visit.Id,
            DoctorId = ApiFactory.SuperUserId,
            WorkflowStatusId = status.Id,
            EntryAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        dbContext.QueueEntries.Add(new QueueEntry
        {
            PatientId = patient.Id,
            VisitId = visit.Id,
            DoctorId = ApiFactory.SuperUserId,
            WorkflowStatusId = status.Id,
            EntryAtUtc = DateTime.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Visit_ShouldAllowOnlyOneActiveSession()
    {
        await using var scope = CreateClinicScope(ApiFactory.ClinicAId, ApiFactory.SuperUserId);
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var patient = await dbContext.Patients.SingleAsync();
        var visit = new Visit
        {
            PatientId = patient.Id,
            DoctorId = ApiFactory.SuperUserId,
            EntryAtUtc = DateTime.UtcNow
        };
        dbContext.Visits.Add(visit);
        await dbContext.SaveChangesAsync();

        dbContext.VisitSessions.Add(new VisitSession
        {
            VisitId = visit.Id,
            DoctorId = ApiFactory.SuperUserId,
            CreatedByUserId = ApiFactory.SuperUserId,
            StartedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        dbContext.VisitSessions.Add(new VisitSession
        {
            VisitId = visit.Id,
            DoctorId = ApiFactory.SuperUserId,
            CreatedByUserId = ApiFactory.SuperUserId,
            StartedAtUtc = DateTime.UtcNow.AddMinutes(1)
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    private AsyncServiceScope CreateClinicScope(Guid clinicId, Guid userId)
    {
        var scope = factory.Services.CreateAsyncScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("clinic_id", clinicId.ToString()),
                new Claim("user_id", userId.ToString()),
                new Claim("super_user", "true")
            ], "Test"))
        };
        return scope;
    }
}
