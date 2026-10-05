using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Patients;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DomainClinic = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.IntegrationTests;

public sealed class PatientApiTests
{
    [Fact]
    public async Task CreateAndList_Patient_StaysInsideAuthenticatedClinic()
    {
        await using var factory = new ApiFactory();
        var clinicId = await CreateClinicAsync(factory);
        var account = await CreateSuperUserAsync(factory, clinicId);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, account);

        var createResponse = await client.PostAsJsonAsync(
            "/api/patients",
            new CreatePatientRequest
            {
                FullName = "Ahmed Patient",
                Phone = "+20 100 123 4567",
                Gender = "Male",
                DateOfBirth = new DateOnly(1990, 5, 10)
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdEnvelope = await createResponse.Content.ReadFromJsonAsync<BaseResponse<PatientResponse>>();
        Assert.NotNull(createdEnvelope?.Data);
        Assert.StartsWith("P-", createdEnvelope.Data.PatientNumber, StringComparison.Ordinal);
        Assert.Equal("+201001234567", createdEnvelope.Data.Phone);

        var listResponse = await client.GetAsync("/api/patients?search=Ahmed&page=1&pageSize=20");
        listResponse.EnsureSuccessStatusCode();
        var listEnvelope = await listResponse.Content.ReadFromJsonAsync<BaseResponse<PaginatedResponse<PatientResponse>>>();

        Assert.NotNull(listEnvelope?.Data);
        Assert.Contains(listEnvelope.Data.Data, patient => patient.Id == createdEnvelope.Data.Id);
        Assert.Equal(1, listEnvelope.Data.Setting.TotalCount);
    }

    [Fact]
    public async Task DuplicateCheck_AfterCreate_ReturnsPhoneCandidate()
    {
        await using var factory = new ApiFactory();
        var clinicId = await CreateClinicAsync(factory);
        var account = await CreateSuperUserAsync(factory, clinicId);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, account);

        var request = new CreatePatientRequest
        {
            FullName = "Duplicate Candidate",
            Phone = "0100 555 2211",
            DateOfBirth = new DateOnly(1988, 2, 4)
        };

        var createResponse = await client.PostAsJsonAsync("/api/patients", request);
        createResponse.EnsureSuccessStatusCode();

        var duplicateResponse = await client.PostAsJsonAsync(
            "/api/patients/duplicates",
            new PatientDuplicateCheckRequest
            {
                FullName = request.FullName,
                Phone = request.Phone,
                DateOfBirth = request.DateOfBirth
            });

        duplicateResponse.EnsureSuccessStatusCode();
        var envelope = await duplicateResponse.Content
            .ReadFromJsonAsync<BaseResponse<PatientDuplicateCheckResponse>>();

        Assert.NotNull(envelope?.Data);
        Assert.True(envelope.Data.HasPotentialDuplicates);
        var candidate = Assert.Single(envelope.Data.Candidates);
        Assert.Contains("phone", candidate.MatchReasons);
        Assert.Contains("name_and_date_of_birth", candidate.MatchReasons);
    }

    [Fact]
    public async Task Get_CrossClinicPatient_ReturnsNotFound()
    {
        await using var factory = new ApiFactory();
        var callerClinicId = await CreateClinicAsync(factory);
        var otherClinicId = await CreateClinicAsync(factory);
        var account = await CreateSuperUserAsync(factory, callerClinicId);
        var otherPatientId = await SeedPatientAsync(factory, otherClinicId);

        using var client = factory.CreateClient();
        await AuthenticateAsync(client, account);

        var response = await client.GetAsync($"/api/patients/details?patientId={otherPatientId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
            Name = $"Patient API Clinic {suffix}",
            Code = $"PAT-{suffix}",
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
        var email = $"patient-api-{suffix}@auran.local";
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
            FullName = $"Patient API Admin {suffix}",
            Email = email,
            IsSuperUser = true,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        return new TestCredentials(email, password);
    }

    private static async Task<Guid> SeedPatientAsync(ApiFactory factory, Guid clinicId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var id = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        dbContext.Patients.Add(new Patient
        {
            Id = id,
            ClinicId = clinicId,
            PatientNumber = $"P-SEED-{suffix}",
            FullName = $"Other Clinic Patient {suffix}",
            Phone = $"+2010{Random.Shared.Next(10000000, 99999999)}",
            CreatedDate = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        return id;
    }

    private sealed record TestCredentials(string Email, string Password);
}
