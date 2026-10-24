using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Reporting;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using DomainClinic = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.IntegrationTests;

public sealed class DashboardApiTests
{
    [Fact]
    public async Task WeeklyVisits_UsesClinicLocalDaysAndDoesNotIncludeAnotherClinic()
    {
        await using var factory = new ApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
        var clinicId = Guid.NewGuid();
        var otherClinicId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var otherPatientId = Guid.NewGuid();
        var email = $"dashboard-{userId:N}@example.invalid";
        const string password = "DashboardTestPass1!";

        db.Clinics.AddRange(
            new DomainClinic
            {
                Id = clinicId,
                Name = "Dashboard test clinic",
                Code = $"DB-{clinicId:N}"[..15],
                TimeZoneId = "Africa/Cairo"
            },
            new DomainClinic
            {
                Id = otherClinicId,
                Name = "Other dashboard clinic",
                Code = $"DB-{otherClinicId:N}"[..15],
                TimeZoneId = "Africa/Cairo"
            });

        var identity = new ApplicationIdentityUser { UserName = email, Email = email };
        var otherIdentity = new ApplicationIdentityUser
        {
            UserName = $"other-{otherUserId:N}@example.invalid",
            Email = $"other-{otherUserId:N}@example.invalid"
        };
        Assert.True((await userManager.CreateAsync(identity, password)).Succeeded);
        Assert.True((await userManager.CreateAsync(otherIdentity, password)).Succeeded);

        db.Users.AddRange(
            new User
            {
                Id = userId,
                ClinicId = clinicId,
                IdentityUserId = identity.Id,
                FullName = "Dashboard actor",
                Email = email,
                IsSuperUser = true
            },
            new User
            {
                Id = otherUserId,
                ClinicId = otherClinicId,
                IdentityUserId = otherIdentity.Id,
                FullName = "Other clinic doctor"
            });
        db.Patients.AddRange(
            new Patient
            {
                Id = patientId,
                ClinicId = clinicId,
                PatientNumber = $"P-{patientId:N}",
                FullName = "Dashboard patient",
                Phone = "1000000000"
            },
            new Patient
            {
                Id = otherPatientId,
                ClinicId = otherClinicId,
                PatientNumber = $"P-{otherPatientId:N}",
                FullName = "Other patient",
                Phone = "2000000000"
            });

        DateTime LocalTime(DateOnly date, int hour, int minute) =>
            TimeZoneInfo.ConvertTimeToUtc(
                date.ToDateTime(new TimeOnly(hour, minute)), timeZone);

        db.Visits.AddRange(
            new Visit
            {
                Id = Guid.NewGuid(), ClinicId = clinicId, PatientId = patientId,
                DoctorId = userId, EntryAtUtc = LocalTime(today.AddDays(-1), 23, 50),
                CompletedAtUtc = LocalTime(today, 0, 5), Status = VisitStatus.Completed
            },
            new Visit
            {
                Id = Guid.NewGuid(), ClinicId = clinicId, PatientId = patientId,
                DoctorId = userId, EntryAtUtc = LocalTime(today, 0, 10),
                CompletedAtUtc = LocalTime(today, 0, 15), Status = VisitStatus.Completed
            },
            new Visit
            {
                Id = Guid.NewGuid(), ClinicId = clinicId, PatientId = patientId,
                DoctorId = userId, EntryAtUtc = LocalTime(today.AddDays(-7), 12, 0)
            },
            new Visit
            {
                Id = Guid.NewGuid(), ClinicId = otherClinicId, PatientId = otherPatientId,
                DoctorId = otherUserId, EntryAtUtc = LocalTime(today, 0, 20)
            });
        await db.SaveChangesAsync();

        try
        {
            using var client = factory.CreateClient();
            var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
            {
                Email = email,
                Password = password
            });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            var session = await login.Content.ReadFromJsonAsync<BaseResponse<AuthResponse>>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", session!.Data!.AccessToken);

            var response = await client.GetAsync("/api/reporting/dashboard");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<BaseResponse<DashboardSummaryResponse>>();
            var dashboard = Assert.IsType<DashboardSummaryResponse>(result!.Data);
            Assert.Equal(today, dashboard.LocalDate);
            Assert.Equal(1, dashboard.VisitsToday);
            Assert.Equal(2, dashboard.CompletedVisitsToday);
            Assert.Equal(1, dashboard.CompletedVisitsStartedToday);
            Assert.Equal(7, dashboard.WeeklyVisits.Count);
            Assert.Equal(today.AddDays(-6), dashboard.WeeklyVisits[0].LocalDate);
            Assert.Equal(today, dashboard.WeeklyVisits[6].LocalDate);
            Assert.Equal(1, dashboard.WeeklyVisits[5].Count);
            Assert.Equal(1, dashboard.WeeklyVisits[6].Count);
            Assert.Equal(2, dashboard.WeeklyVisits.Sum(day => day.Count));
        }
        finally
        {
            db.RefreshTokens.RemoveRange(db.RefreshTokens.Where(token =>
                token.ClinicId == clinicId || token.ClinicId == otherClinicId));
            db.Visits.RemoveRange(db.Visits.Where(visit =>
                visit.ClinicId == clinicId || visit.ClinicId == otherClinicId));
            db.Patients.RemoveRange(db.Patients.Where(patient =>
                patient.ClinicId == clinicId || patient.ClinicId == otherClinicId));
            db.Users.RemoveRange(db.Users.Where(user =>
                user.ClinicId == clinicId || user.ClinicId == otherClinicId));
            await db.SaveChangesAsync();
            db.Clinics.RemoveRange(db.Clinics.Where(clinic =>
                clinic.Id == clinicId || clinic.Id == otherClinicId));
            await db.SaveChangesAsync();
            await userManager.DeleteAsync(identity);
            await userManager.DeleteAsync(otherIdentity);
        }
    }
}
