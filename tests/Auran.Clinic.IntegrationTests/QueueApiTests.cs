using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Patients;
using Auran.Clinic.Application.Queue;
using Auran.Clinic.Application.Users;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using DomainClinic = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.IntegrationTests;

public sealed class QueueApiTests
{
    [Fact]
    public async Task CheckInAndMove_UsesConfiguredWorkflowAndConcurrencyVersion()
    {
        await using var factory = new ApiFactory();
        var clinicId = await CreateClinicAsync(factory);
        var account = await CreateSuperUserAsync(factory, clinicId);
        var workflow = await SeedWorkflowAsync(factory, clinicId);

        using var client = factory.CreateClient();
        await AuthenticateAsync(client, account);

        var doctorResponse = await client.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest
            {
                FullName = "Queue Doctor",
                Email = $"queue-doctor-{Guid.NewGuid():N}@auran.local",
                Password = "ValidPassword1",
                Roles = [SystemRoleCatalog.Doctor]
            });
        Assert.Equal(HttpStatusCode.Created, doctorResponse.StatusCode);
        var doctorEnvelope = await doctorResponse.Content.ReadFromJsonAsync<BaseResponse<UserAccountResponse>>();
        Assert.NotNull(doctorEnvelope?.Data);

        var patientResponse = await client.PostAsJsonAsync(
            "/api/patients",
            new CreatePatientRequest
            {
                FullName = "Queue Patient",
                Phone = $"010{Random.Shared.Next(10000000, 99999999)}"
            });
        Assert.Equal(HttpStatusCode.Created, patientResponse.StatusCode);
        var patientEnvelope = await patientResponse.Content.ReadFromJsonAsync<BaseResponse<PatientResponse>>();
        Assert.NotNull(patientEnvelope?.Data);

        var checkInResponse = await client.PostAsJsonAsync(
            "/api/queue/check-in",
            new QueueCheckInRequest
            {
                PatientId = patientEnvelope.Data.Id,
                DoctorId = doctorEnvelope.Data.Id
            });
        Assert.Equal(HttpStatusCode.Created, checkInResponse.StatusCode);
        var checkInEnvelope = await checkInResponse.Content.ReadFromJsonAsync<BaseResponse<QueueEntryResponse>>();
        Assert.NotNull(checkInEnvelope?.Data);
        Assert.Equal(workflow.WaitingStatusId, checkInEnvelope.Data.WorkflowStatusId);
        Assert.False(string.IsNullOrWhiteSpace(checkInEnvelope.Data.RowVersion));

        var boardResponse = await client.GetAsync("/api/queue");
        boardResponse.EnsureSuccessStatusCode();
        var boardEnvelope = await boardResponse.Content.ReadFromJsonAsync<BaseResponse<QueueBoardResponse>>();
        Assert.NotNull(boardEnvelope?.Data);
        Assert.Contains(boardEnvelope.Data.Entries, entry => entry.Id == checkInEnvelope.Data.Id);
        Assert.Contains(boardEnvelope.Data.Staff, staff => staff.Id == doctorEnvelope.Data.Id);
        Assert.Contains(
            boardEnvelope.Data.Transitions,
            transition =>
                transition.FromStatusId == workflow.WaitingStatusId &&
                transition.ToStatusId == workflow.ReadyStatusId);

        var moveResponse = await client.PutAsJsonAsync(
            "/api/queue/move",
            new QueueMoveRequest
            {
                QueueEntryId = checkInEnvelope.Data.Id,
                ToStatusId = workflow.ReadyStatusId,
                RowVersion = checkInEnvelope.Data.RowVersion
            });
        moveResponse.EnsureSuccessStatusCode();
        var movedEnvelope = await moveResponse.Content.ReadFromJsonAsync<BaseResponse<QueueEntryResponse>>();
        Assert.NotNull(movedEnvelope?.Data);
        Assert.Equal(workflow.ReadyStatusId, movedEnvelope.Data.WorkflowStatusId);

        var staleMoveResponse = await client.PutAsJsonAsync(
            "/api/queue/move",
            new QueueMoveRequest
            {
                QueueEntryId = checkInEnvelope.Data.Id,
                ToStatusId = workflow.ReadyStatusId,
                RowVersion = checkInEnvelope.Data.RowVersion
            });

        Assert.True(
            staleMoveResponse.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict,
            $"Unexpected stale move status: {staleMoveResponse.StatusCode}");
    }

    private static async Task<WorkflowIds> SeedWorkflowAsync(ApiFactory factory, Guid clinicId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var waitingId = Guid.NewGuid();
        var readyId = Guid.NewGuid();

        dbContext.WorkflowStatuses.AddRange(
            new WorkflowStatus
            {
                Id = waitingId,
                ClinicId = clinicId,
                Code = "WAITING",
                Name = "Waiting",
                Color = "#F59E0B",
                SortOrder = 10,
                IsSystemFinal = false,
                CreatedDate = DateTime.UtcNow
            },
            new WorkflowStatus
            {
                Id = readyId,
                ClinicId = clinicId,
                Code = "READY",
                Name = "Ready",
                Color = "#10B981",
                SortOrder = 20,
                IsSystemFinal = false,
                CreatedDate = DateTime.UtcNow
            });

        dbContext.WorkflowTransitions.Add(new WorkflowTransition
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            FromStatusId = waitingId,
            ToStatusId = readyId,
            CreatedDate = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return new WorkflowIds(waitingId, readyId);
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
            Name = $"Queue API Clinic {suffix}",
            Code = $"QUE-{suffix}",
            IsActive = true
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
        var email = $"queue-api-{suffix}@auran.local";
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
            FullName = $"Queue API Admin {suffix}",
            Email = email,
            IsSuperUser = true,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        return new TestCredentials(email, password);
    }

    private sealed record WorkflowIds(Guid WaitingStatusId, Guid ReadyStatusId);
    private sealed record TestCredentials(string Email, string Password);
}
