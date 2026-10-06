using System.Net.Http.Headers;
using System.Net.Http.Json;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Patients;
using Auran.Clinic.Application.Settings;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using DomainClinic = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.IntegrationTests;

public sealed class DynamicFieldSettingsApiTests
{
    [Fact]
    public async Task SavedFieldConfiguration_IsReflectedInPatientProfileAndMeasurements()
    {
        await using var factory = new ApiFactory();
        var clinicId = await CreateClinicAsync(factory);
        var account = await CreateSuperUserAsync(factory, clinicId);

        using var client = factory.CreateClient();
        await AuthenticateAsync(client, account);

        var patientResponse = await client.PostAsJsonAsync(
            "/api/patients",
            new CreatePatientRequest
            {
                FullName = "Dynamic Settings Patient",
                Phone = $"015{Random.Shared.Next(10000000, 99999999)}"
            });
        patientResponse.EnsureSuccessStatusCode();
        var patient = await patientResponse.Content.ReadFromJsonAsync<BaseResponse<PatientResponse>>();
        Assert.NotNull(patient?.Data);

        var saveResponse = await client.PutAsJsonAsync(
            "/api/settings/fields",
            new SaveFieldSettingsRequest
            {
                ProfileSections =
                [
                    new SavePatientProfileSectionSettingsRequest
                    {
                        Name = "Lifestyle",
                        SortOrder = 10,
                        IsEnabled = true,
                        Fields =
                        [
                            new SavePatientProfileFieldSettingsRequest
                            {
                                Label = "Smoking",
                                FieldType = DynamicFieldType.SingleSelect,
                                IsRequired = false,
                                IsEnabled = true,
                                SortOrder = 10,
                                Options =
                                [
                                    new SaveFieldOptionSettingsRequest { Label = "No", Value = "no", SortOrder = 10 },
                                    new SaveFieldOptionSettingsRequest { Label = "Yes", Value = "yes", SortOrder = 20 }
                                ]
                            }
                        ]
                    }
                ],
                ClinicalFields =
                [
                    new SaveClinicalFieldSettingsRequest
                    {
                        Name = "Temperature",
                        FieldType = DynamicFieldType.Number,
                        Unit = "C",
                        IsEnabled = true,
                        SortOrder = 10
                    }
                ]
            });
        saveResponse.EnsureSuccessStatusCode();

        var saved = await saveResponse.Content.ReadFromJsonAsync<BaseResponse<FieldSettingsResponse>>();
        Assert.NotNull(saved?.Data);
        Assert.Contains(saved.Data.ProfileSections, section =>
            section.Name == "Lifestyle" &&
            section.Fields.Any(field =>
                field.Label == "Smoking" &&
                field.Options.Count == 2));
        Assert.Contains(saved.Data.ClinicalFields, field =>
            field.Name == "Temperature" &&
            field.Unit == "C");

        var profileResponse = await client.PostAsJsonAsync(
            "/api/patient-clinical-data/dynamic-profile/details",
            new PatientLookupRequest { PatientId = patient.Data.Id });
        profileResponse.EnsureSuccessStatusCode();
        var profile = await profileResponse.Content.ReadFromJsonAsync<BaseResponse<PatientDynamicProfileResponse>>();
        Assert.NotNull(profile?.Data);
        Assert.Contains(profile.Data.Sections, section =>
            section.Name == "Lifestyle" &&
            section.Fields.Any(field => field.Label == "Smoking"));

        var measurementsResponse = await client.PostAsJsonAsync(
            "/api/patient-clinical-data/measurements/details",
            new PatientLookupRequest { PatientId = patient.Data.Id });
        measurementsResponse.EnsureSuccessStatusCode();
        var measurements = await measurementsResponse.Content.ReadFromJsonAsync<BaseResponse<PatientMeasurementsResponse>>();
        Assert.NotNull(measurements?.Data);
        Assert.Contains(measurements.Data.Fields, field =>
            field.Name == "Temperature" &&
            field.Unit == "C");
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
            Name = $"Dynamic Settings Clinic {suffix}",
            Code = $"DFC-{suffix}",
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
        var email = $"dynamic-fields-{suffix}@auran.local";
        const string password = "ValidPassword1";

        var identityUser = new ApplicationIdentityUser
        {
            UserName = email,
            Email = email,
            LockoutEnabled = true
        };

        var identityResult = await userManager.CreateAsync(identityUser, password);
        Assert.True(identityResult.Succeeded);

        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            IdentityUserId = identityUser.Id,
            FullName = $"Dynamic Fields Admin {suffix}",
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
