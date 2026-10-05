using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.FollowUps;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Patients;
using Auran.Clinic.Application.Queue;
using Auran.Clinic.Application.Users;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using DomainClinic = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.IntegrationTests;

public sealed class FollowUpApiTests
{
    [Fact]
    public async Task CreateFilterAndComplete_FollowUp_WorksAcrossQueueVisitFlow()
    {
        await using var factory = new ApiFactory();
        var clinicId = await CreateClinicAsync(factory);
        await SeedInitialWorkflowAsync(factory, clinicId);
        var account = await CreateSuperUserAsync(factory, clinicId);

        using var client = factory.CreateClient();
        await AuthenticateAsync(client, account);

        var doctorResponse = await client.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest
            {
                FullName = "Follow-up Doctor",
                Email = $"followup-doctor-{Guid.NewGuid():N}@auran.local",
                Password = "ValidPassword1",
                Roles = [SystemRoleCatalog.Doctor]
            });
        Assert.Equal(HttpStatusCode.Created, doctorResponse.StatusCode);
        var doctor = await doctorResponse.Content.ReadFromJsonAsync<BaseResponse<UserAccountResponse>>();
        Assert.NotNull(doctor?.Data);

        var patientResponse = await client.PostAsJsonAsync(
            "/api/patients",
            new CreatePatientRequest
            {
                FullName = "Follow-up Patient",
                Phone = $"011{Random.Shared.Next(10000000, 99999999)}"
            });
        Assert.Equal(HttpStatusCode.Created, patientResponse.StatusCode);
        var patient = await patientResponse.Content.ReadFromJsonAsync<BaseResponse<PatientResponse>>();
        Assert.NotNull(patient?.Data);

        var checkInResponse = await client.PostAsJsonAsync(
            "/api/queue/check-in",
            new QueueCheckInRequest
            {
                PatientId = patient.Data.Id,
                DoctorId = doctor.Data.Id
            });
        Assert.Equal(HttpStatusCode.Created, checkInResponse.StatusCode);
        var queueEntry = await checkInResponse.Content.ReadFromJsonAsync<BaseResponse<QueueEntryResponse>>();
        Assert.NotNull(queueEntry?.Data);

        var recommendedDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        var createResponse = await client.PostAsJsonAsync(
            "/api/follow-ups",
            new CreateFollowUpRequest
            {
                PatientId = patient.Data.Id,
                VisitId = queueEntry.Data.VisitId,
                DoctorId = doctor.Data.Id,
                Recommendation = "Review response to treatment.",
                RecommendedDate = recommendedDate
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<BaseResponse<FollowUpResponse>>();
        Assert.NotNull(created?.Data);
        Assert.Equal("Upcoming", created.Data.DueCategory);

        var listResponse = await client.GetAsync("/api/follow-ups?dueCategory=Upcoming");
        listResponse.EnsureSuccessStatusCode();
        var list = await listResponse.Content.ReadFromJsonAsync<BaseResponse<IReadOnlyCollection<FollowUpResponse>>>();
        Assert.NotNull(list?.Data);
        Assert.Contains(list.Data, item => item.Id == created.Data.Id);

        var completeResponse = await client.PutAsJsonAsync(
            "/api/follow-ups/status",
            new SetFollowUpStatusRequest
            {
                FollowUpId = created.Data.Id,
                Status = FollowUpStatus.Completed
            });
        completeResponse.EnsureSuccessStatusCode();
        var completed = await completeResponse.Content.ReadFromJsonAsync<BaseResponse<FollowUpResponse>>();
        Assert.NotNull(completed?.Data);
        Assert.Equal(FollowUpStatus.Completed, completed.Data.Status);
        Assert.Equal("Completed", completed.Data.DueCategory);
    }

    private static async Task SeedInitialWorkflowAsync(ApiFactory factory, Guid clinicId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();

        dbContext.WorkflowStatuses.Add(new WorkflowStatus
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            Code = "WAITING",
            Name = "Waiting",
            Color = "#F59E0B",
            SortOrder = 10,
            IsSystemFinal = false,
            CreatedDate = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task AuthenticateAsync(HttpClient client, TestCredentials credentials)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest
            {
                Email = credentials.Email,
                Password = credentials.Password
            });
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<BaseResponse<AuthResponse>>();
        Assert.NotNull(envelope?.Data);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", envelope.Data.AccessToken);
    }

    private static async Task<Guid> CreateClinicAsync(ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var clinicId = Guid.NewGuid();

        dbContext.Clinics.Add(new DomainClinic
        {
            Id = clinicId,
            Name = $"Follow-up Clinic {suffix}",
            Code = $"FUP-{suffix}",
            IsActive = true,
            TimeZoneId = "Africa/Cairo"
        });
        await dbContext.SaveChangesAsync();
        return clinicId;
    }

    private static async Task<TestCredentials> CreateSuperUserAsync(ApiFactory factory, Guid clinicId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var email = $"followup-api-{suffix}@auran.local";
        const string password = "ValidPassword1";

        var identityUser = new ApplicationIdentityUser
        {
            UserName = email,
            Email = email,
            LockoutEnabled = true
        };
        var identityResult = await userManager.CreateAsync(identityUser, password);
        Assert.True(
            identityResult.Succeeded,
            string.Join(", ", identityResult.Errors.Select(error => error.Description)));

        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            IdentityUserId = identityUser.Id,
            FullName = $"Follow-up API Admin {suffix}",
            Email = email,
            IsSuperUser = true,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        return new TestCredentials(email, password);
    }

    private sealed record TestCredentials(string Email, string Password);
}
