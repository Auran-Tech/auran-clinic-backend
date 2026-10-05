using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.ClinicalOrders;
using Auran.Clinic.Application.Dashboard;
using Auran.Clinic.Application.Files;
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

public sealed class ClinicalWorkflowApiTests
{
    [Fact]
    public async Task ClinicalOrdersFilesAndDashboard_WorkAcrossVisitFlow()
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
                FullName = "Clinical Workflow Doctor",
                Email = $"clinical-doctor-{Guid.NewGuid():N}@auran.local",
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
                FullName = "Clinical Workflow Patient",
                Phone = $"012{Random.Shared.Next(10000000, 99999999)}"
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
        var queue = await checkInResponse.Content.ReadFromJsonAsync<BaseResponse<QueueEntryResponse>>();
        Assert.NotNull(queue?.Data);

        var definitionResponse = await client.PutAsJsonAsync(
            "/api/clinical-orders/definitions",
            new SaveClinicalOrderSectionDefinitionsRequest
            {
                Sections =
                [
                    new SaveClinicalOrderSectionDefinitionRequest
                    {
                        Code = "PRESCRIPTION",
                        Name = "Prescription",
                        SectionType = ClinicalOrderSectionType.Structured,
                        SortOrder = 10,
                        IsEnabled = true
                    },
                    new SaveClinicalOrderSectionDefinitionRequest
                    {
                        Code = "PLAN",
                        Name = "Plan",
                        SectionType = ClinicalOrderSectionType.Text,
                        SortOrder = 20,
                        IsEnabled = true
                    }
                ]
            });
        definitionResponse.EnsureSuccessStatusCode();

        var saveOrderResponse = await client.PutAsJsonAsync(
            "/api/clinical-orders",
            new SaveClinicalOrderRequest
            {
                VisitId = queue.Data.VisitId,
                Sections =
                [
                    new SaveClinicalOrderSectionRequest
                    {
                        DefinitionCode = "PRESCRIPTION",
                        Items =
                        [
                            new SaveClinicalOrderItemRequest
                            {
                                Name = "Amoxicillin 500mg",
                                DetailsJson = "{\"frequency\":\"TID\"}"
                            }
                        ]
                    },
                    new SaveClinicalOrderSectionRequest
                    {
                        DefinitionCode = "PLAN",
                        TextValue = "Review symptoms in one week."
                    }
                ]
            });
        saveOrderResponse.EnsureSuccessStatusCode();
        var order = await saveOrderResponse.Content.ReadFromJsonAsync<BaseResponse<ClinicalOrderResponse>>();
        Assert.NotNull(order?.Data);
        Assert.Equal(2, order.Data.Sections.Count);
        Assert.Contains(order.Data.Sections, section =>
            section.DefinitionCode == "PRESCRIPTION" &&
            section.Items.Any(item => item.Name == "Amoxicillin 500mg"));

        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent(patient.Data.Id.ToString()), "PatientId");
        multipart.Add(new StringContent("Referral"), "Category");
        multipart.Add(new StringContent("Integration test attachment"), "Notes");
        var fileBytes = Encoding.UTF8.GetBytes("Auran clinical attachment test.");
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        multipart.Add(fileContent, "File", "clinical-note.txt");

        var uploadResponse = await client.PostAsync("/api/files/patient/upload", multipart);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<BaseResponse<FileAttachmentResponse>>();
        Assert.NotNull(uploaded?.Data);
        Assert.Equal("clinical-note.txt", uploaded.Data.OriginalName);
        Assert.Equal("Referral", uploaded.Data.Category);

        var listResponse = await client.PostAsJsonAsync(
            "/api/files/patient/list",
            new PatientAttachmentLookupRequest { PatientId = patient.Data.Id });
        listResponse.EnsureSuccessStatusCode();
        var files = await listResponse.Content.ReadFromJsonAsync<BaseResponse<IReadOnlyCollection<FileAttachmentResponse>>>();
        Assert.NotNull(files?.Data);
        Assert.Contains(files.Data, item => item.FileId == uploaded.Data.FileId);

        var downloadResponse = await client.PostAsJsonAsync(
            "/api/files/download",
            new FileDownloadRequest { FileId = uploaded.Data.FileId });
        downloadResponse.EnsureSuccessStatusCode();
        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(fileBytes, downloadedBytes);

        var dashboardResponse = await client.GetAsync("/api/dashboard");
        dashboardResponse.EnsureSuccessStatusCode();
        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<BaseResponse<DashboardResponse>>();
        Assert.NotNull(dashboard?.Data);
        Assert.True(dashboard.Data.TotalPatients >= 1);
        Assert.True(dashboard.Data.ActiveQueueCount >= 1);
        Assert.True(dashboard.Data.OpenVisitsCount >= 1);
        Assert.Contains(dashboard.Data.Queue, item => item.PatientNumber == patient.Data.PatientNumber);
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
            Name = $"Clinical Workflow Clinic {suffix}",
            Code = $"CWF-{suffix}",
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
        var email = $"clinical-api-{suffix}@auran.local";
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
            FullName = $"Clinical API Admin {suffix}",
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
