using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Patients;
using Auran.Clinic.Application.Queue;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DomainClinic = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.IntegrationTests;

public sealed class PatientQueueSortingApiTests
{
    [Fact]
    public async Task Lists_FilterAndSortOnServerWithinAuthenticatedClinic()
    {
        await using var factory = new ApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();
        var clinicId = Guid.NewGuid();
        var otherClinicId = Guid.NewGuid();
        var email = $"sort-test-{Guid.NewGuid():N}@example.invalid";
        const string password = "ValidPassword1!";
        var identity = new ApplicationIdentityUser { UserName = email, Email = email };
        Assert.True((await userManager.CreateAsync(identity, password)).Succeeded);

        var actor = new User
        {
            Id = Guid.NewGuid(), ClinicId = clinicId, IdentityUserId = identity.Id,
            FullName = "Sort actor", Email = email, IsSuperUser = true
        };
        var alice = NewPatient(clinicId, "Alice", "Female", 1);
        var bob = NewPatient(clinicId, "Bob", "Male", 2);
        var cara = NewPatient(clinicId, "Cara", "Female", 3);
        var foreign = NewPatient(otherClinicId, "Zoe", "Female", 4);
        var waiting = new WorkflowStatus
        {
            Id = Guid.NewGuid(), ClinicId = clinicId, Code = "WAITING_SORT_TEST",
            Name = "Waiting", Color = "#445566", SortOrder = 0
        };
        var ready = new WorkflowStatus
        {
            Id = Guid.NewGuid(), ClinicId = clinicId, Code = "READY_SORT_TEST",
            Name = "Ready", Color = "#778899", SortOrder = 1
        };

        db.Clinics.AddRange(
            new DomainClinic { Id = clinicId, Name = "Sort clinic", Code = $"QS-{clinicId:N}"[..15] },
            new DomainClinic { Id = otherClinicId, Name = "Other clinic", Code = $"QO-{otherClinicId:N}"[..15] });
        db.Users.Add(actor);
        db.Patients.AddRange(alice, bob, cara, foreign);
        db.WorkflowStatuses.AddRange(waiting, ready);
        await db.SaveChangesAsync();

        foreach (var (patient, stage, minutes) in new[]
        {
            (alice, waiting, 30), (bob, ready, 20), (cara, waiting, 10)
        })
        {
            var visitId = Guid.NewGuid();
            var entryAt = DateTime.UtcNow.AddMinutes(-minutes);
            db.Visits.Add(new Visit
            {
                Id = visitId, ClinicId = clinicId, PatientId = patient.Id,
                DoctorId = actor.Id, EntryAtUtc = entryAt
            });
            db.QueueEntries.Add(new QueueEntry
            {
                Id = Guid.NewGuid(), ClinicId = clinicId, PatientId = patient.Id,
                VisitId = visitId, DoctorId = actor.Id,
                WorkflowStatusId = stage.Id, EntryAtUtc = entryAt,
                ExitAtUtc = patient.Id == cara.Id ? DateTime.UtcNow.AddMinutes(-5) : null
            });
        }
        await db.SaveChangesAsync();

        try
        {
            using var client = factory.CreateClient();
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await client.GetAsync("/api/patients")).StatusCode);

            var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
            {
                Email = email, Password = password
            });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            var session = await login.Content.ReadFromJsonAsync<BaseResponse<AuthResponse>>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", session!.Data!.AccessToken);

            var first = await client.GetFromJsonAsync<BaseResponse<PaginatedResponse<PatientResponse>>>(
                "/api/patients?gender=Female&sortBy=createdDate&descending=true&page=1&pageSize=1");
            Assert.Equal(2, first!.Data!.Setting.TotalCount);
            Assert.Equal(cara.Id, Assert.Single(first.Data.Data).Id);

            var second = await client.GetFromJsonAsync<BaseResponse<PaginatedResponse<PatientResponse>>>(
                "/api/patients?gender=Female&sortBy=createdDate&descending=true&page=2&pageSize=1");
            Assert.Equal(alice.Id, Assert.Single(second!.Data!.Data).Id);

            var queue = await client.GetFromJsonAsync<BaseResponse<List<QueueEntryResponse>>>(
                "/api/queue?sortBy=patientName&descending=true");
            Assert.Equal(new[] { bob.Id, alice.Id },
                queue!.Data!.Select(item => item.PatientId));

            var recentQueue = await client.GetFromJsonAsync<BaseResponse<List<QueueEntryResponse>>>(
                "/api/queue?includeRecentExits=true&sortBy=patientName&descending=true");
            Assert.Equal(new[] { cara.Id, bob.Id, alice.Id },
                recentQueue!.Data!.Select(item => item.PatientId));
            Assert.NotNull(recentQueue.Data![0].ExitAtUtc);

            Assert.Equal(HttpStatusCode.BadRequest,
                (await client.GetAsync("/api/patients?sortBy=unknown")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,
                (await client.GetAsync("/api/queue?sortBy=unknown")).StatusCode);
        }
        finally
        {
            db.RefreshTokens.RemoveRange(db.RefreshTokens.Where(item => item.ClinicId == clinicId));
            db.QueueEntries.RemoveRange(db.QueueEntries.Where(item => item.ClinicId == clinicId));
            await db.SaveChangesAsync();
            db.Visits.RemoveRange(db.Visits.Where(item => item.ClinicId == clinicId));
            db.WorkflowStatuses.RemoveRange(db.WorkflowStatuses.Where(item => item.ClinicId == clinicId));
            db.Patients.RemoveRange(db.Patients.Where(item =>
                item.ClinicId == clinicId || item.ClinicId == otherClinicId));
            db.Users.Remove(actor);
            await db.SaveChangesAsync();
            db.Clinics.RemoveRange(db.Clinics.Where(item =>
                item.Id == clinicId || item.Id == otherClinicId));
            await db.SaveChangesAsync();
            await userManager.DeleteAsync(identity);
        }
    }

    private static Patient NewPatient(Guid clinicId, string name, string gender, int day) =>
        new()
        {
            Id = Guid.NewGuid(), ClinicId = clinicId, PatientNumber = $"SORT-{Guid.NewGuid():N}"[..20],
            FullName = name, Phone = Guid.NewGuid().ToString("N"), Gender = gender,
            CreatedDate = new DateTime(2026, 1, day, 12, 0, 0, DateTimeKind.Utc)
        };
}
