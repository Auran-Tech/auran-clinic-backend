using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Auran.Clinic.IntegrationTests.ReleaseReadiness;

public sealed class AuthenticatedTenantBoundaryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthenticatedTenantBoundaryTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Valid_clinic_token_cannot_read_another_clinics_visit_history()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var clinicA = Guid.NewGuid();
        var clinicB = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var patientA = Guid.NewGuid();
        var patientB = Guid.NewGuid();
        var identityId = Guid.NewGuid().ToString("N");

        db.Clinics.AddRange(
            new Auran.Clinic.Domain.Entities.Clinic { Id = clinicA, Name = "Tenant test A", Code = "T" + clinicA.ToString("N")[..12] },
            new Auran.Clinic.Domain.Entities.Clinic { Id = clinicB, Name = "Tenant test B", Code = "T" + clinicB.ToString("N")[..12] });
        db.Set<ApplicationIdentityUser>().Add(new ApplicationIdentityUser
        {
            Id = identityId,
            UserName = "test-" + identityId,
            NormalizedUserName = "TEST-" + identityId.ToUpperInvariant(),
            Email = identityId + "@example.invalid",
            NormalizedEmail = identityId.ToUpperInvariant() + "@EXAMPLE.INVALID",
            SecurityStamp = Guid.NewGuid().ToString()
        });
        db.Users.Add(new User
        {
            Id = userId, ClinicId = clinicA, IdentityUserId = identityId,
            FullName = "Tenant test actor", IsActive = true
        });
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = sessionId, ClinicId = clinicA, UserId = userId,
            TokenHash = Guid.NewGuid().ToString("N"),
            ExpiresDate = DateTime.UtcNow.AddHours(1)
        });
        db.Patients.AddRange(
            new Patient { Id = patientA, ClinicId = clinicA, PatientNumber = "A" + patientA.ToString("N")[..12], FullName = "A", Phone = patientA.ToString("N") },
            new Patient { Id = patientB, ClinicId = clinicB, PatientNumber = "B" + patientB.ToString("N")[..12], FullName = "B", Phone = patientB.ToString("N") });
        var doctorB = Guid.NewGuid();
        var identityB = Guid.NewGuid().ToString("N");
        db.Set<ApplicationIdentityUser>().Add(new ApplicationIdentityUser
        {
            Id = identityB, UserName = "test-" + identityB,
            NormalizedUserName = "TEST-" + identityB.ToUpperInvariant(),
            Email = identityB + "@example.invalid",
            NormalizedEmail = identityB.ToUpperInvariant() + "@EXAMPLE.INVALID",
            SecurityStamp = Guid.NewGuid().ToString()
        });
        db.Users.Add(new User { Id = doctorB, ClinicId = clinicB, IdentityUserId = identityB, FullName = "Clinic B doctor", IsActive = true });
        var visitA = Guid.NewGuid();
        var visitB = Guid.NewGuid();
        db.Visits.AddRange(
            new Visit { Id = visitA, ClinicId = clinicA, PatientId = patientA, DoctorId = userId, EntryAtUtc = DateTime.UtcNow },
            new Visit { Id = visitB, ClinicId = clinicB, PatientId = patientB, DoctorId = doctorB, EntryAtUtc = DateTime.UtcNow });
        var fileA = Guid.NewGuid();
        var fileB = Guid.NewGuid();
        var attachmentB = Guid.NewGuid();
        db.Files.AddRange(
            new FileRecord { Id = fileA, ClinicId = clinicA, OriginalName = "a.pdf", StoredName = "a.pdf", ContentType = "application/pdf", Size = 4, StorageProvider = "local", StorageKey = "fixture-a-" + fileA, UploadedAtUtc = DateTime.UtcNow, UploadedByUserId = userId },
            new FileRecord { Id = fileB, ClinicId = clinicB, OriginalName = "b.pdf", StoredName = "b.pdf", ContentType = "application/pdf", Size = 4, StorageProvider = "local", StorageKey = "fixture-b-" + fileB, UploadedAtUtc = DateTime.UtcNow, UploadedByUserId = doctorB });
        db.PatientAttachments.AddRange(
            new PatientAttachment { Id = Guid.NewGuid(), ClinicId = clinicA, PatientId = patientA, FileId = fileA },
            new PatientAttachment { Id = attachmentB, ClinicId = clinicB, PatientId = patientB, FileId = fileB });
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();
        var token = CreateToken(configuration, userId, clinicA, sessionId, "Visit_View");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var own = await client.GetAsync($"/api/visits/history?patientId={patientA}");
        var other = await client.GetAsync($"/api/visits/history?patientId={patientB}");

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, other.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Patient_View"));
        var ownMedicalHistory = await client.GetAsync($"/api/patient-medical-history?patientId={patientA}");
        var otherMedicalHistory = await client.GetAsync($"/api/patient-medical-history?patientId={patientB}");
        Assert.Equal(HttpStatusCode.OK, ownMedicalHistory.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherMedicalHistory.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Visit_View"));
        var medicalHistoryDenied = await client.GetAsync($"/api/patient-medical-history?patientId={patientA}");
        Assert.Equal(HttpStatusCode.Forbidden, medicalHistoryDenied.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Visit_View"));
        var ownMeasurements = await client.GetAsync($"/api/clinical-measurements?visitId={visitA}");
        var foreignMeasurements = await client.GetAsync($"/api/clinical-measurements?visitId={visitB}");
        Assert.Equal(HttpStatusCode.OK, ownMeasurements.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreignMeasurements.StatusCode);

        // The order attachment workspace exposes file names/notes and needs Files_View.
        var fileWorkspaceDenied = await client.GetAsync($"/api/clinical-order-attachments?visitId={visitA}");
        Assert.Equal(HttpStatusCode.Forbidden, fileWorkspaceDenied.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Visit_View", "Files_View"));
        var ownFileWorkspace = await client.GetAsync($"/api/clinical-order-attachments?visitId={visitA}");
        var foreignFileWorkspace = await client.GetAsync($"/api/clinical-order-attachments?visitId={visitB}");
        Assert.Equal(HttpStatusCode.OK, ownFileWorkspace.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreignFileWorkspace.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Patient_View"));
        foreach (var endpoint in new[] { "clinical-measurements", "clinical-order-attachments" })
        {
            var deniedResponse = await client.GetAsync($"/api/{endpoint}?visitId={visitA}");
            Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Files_View"));
        var ownAttachments = await client.GetAsync($"/api/patient-attachments?patientId={patientA}");
        Assert.Equal(HttpStatusCode.OK, ownAttachments.StatusCode);
        Assert.Contains(fileA.ToString(), await ownAttachments.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        var foreignAttachments = await client.GetAsync($"/api/patient-attachments?patientId={patientB}");
        Assert.Equal(HttpStatusCode.OK, foreignAttachments.StatusCode);
        var attachmentJson = await foreignAttachments.Content.ReadAsStringAsync();
        Assert.Contains("\"data\":[]", attachmentJson, StringComparison.OrdinalIgnoreCase);

        var foreignDownload = await client.GetAsync($"/api/patient-attachments/download?fileId={fileB}");
        Assert.Equal(HttpStatusCode.NotFound, foreignDownload.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Files_Upload"));
        var foreignDelete = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/patient-attachments")
        {
            Content = JsonContent.Create(new { attachmentId = attachmentB })
        });
        Assert.Equal(HttpStatusCode.NotFound, foreignDelete.StatusCode);

        using var filePayload = new MultipartFormDataContent();
        filePayload.Add(new StringContent(patientB.ToString()), "PatientId");
        using var fileContent = new ByteArrayContent(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        filePayload.Add(fileContent, "File", "test.pdf");
        var foreignUpload = await client.PostAsync("/api/patient-attachments", filePayload);
        Assert.Equal(HttpStatusCode.NotFound, foreignUpload.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Files_View"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Patient_View"));
        var fileReadDenied = await client.GetAsync($"/api/patient-attachments?patientId={patientA}");
        Assert.Equal(HttpStatusCode.Forbidden, fileReadDenied.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Visit_View"));
        foreach (var endpoint in new[] { "clinical-measurements", "clinical-order-attachments" })
        {
            var deniedWrite = await client.PostAsJsonAsync($"/api/{endpoint}", new { visitId = visitB });
            Assert.Equal(HttpStatusCode.Forbidden, deniedWrite.StatusCode);
        }

        // A valid editing permission must never override the clinic boundary.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Visit_Edit"));
        var crossTenantWrite = await client.PostAsJsonAsync(
            "/api/clinical-measurements",
            new { visitId = visitB, values = new[] { new { fieldId = Guid.NewGuid(), textValue = "test" } } });
        Assert.Equal(HttpStatusCode.NotFound, crossTenantWrite.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Visit_Edit", "Files_View"));
        var crossTenantOrderAttachmentWrite = await client.PostAsJsonAsync(
            "/api/clinical-order-attachments",
            new { visitId = visitB, fileId = Guid.NewGuid(), sectionDefinitionId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.NotFound, crossTenantOrderAttachmentWrite.StatusCode);

        // Exercise real local storage and database lifecycle for an authorized patient.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Files_Upload", "Files_View"));
        using var ownPayload = new MultipartFormDataContent();
        ownPayload.Add(new StringContent(patientA.ToString()), "PatientId");
        using var ownFile = new ByteArrayContent(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2d, 0x31 });
        ownFile.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        ownPayload.Add(ownFile, "File", "lifecycle.pdf");
        using var uploaded = await client.PostAsync("/api/patient-attachments", ownPayload);
        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        using var result = JsonDocument.Parse(await uploaded.Content.ReadAsStringAsync());
        var fileId = result.RootElement.GetProperty("data").GetProperty("fileId").GetGuid();
        var attachmentId = result.RootElement.GetProperty("data").GetProperty("attachmentId").GetGuid();
        using var downloaded = await client.GetAsync($"/api/patient-attachments/download?fileId={fileId}");
        Assert.Equal(HttpStatusCode.OK, downloaded.StatusCode);
        Assert.Equal(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2d, 0x31 }, await downloaded.Content.ReadAsByteArrayAsync());
        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/patient-attachments")
        {
            Content = JsonContent.Create(new { attachmentId })
        };
        using var deleted = await client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        using var gone = await client.GetAsync($"/api/patient-attachments/download?fileId={fileId}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId));
        var denied = await client.GetAsync($"/api/visits/history?patientId={patientA}");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    private static string CreateToken(
        IConfiguration config, Guid userId, Guid clinicId, Guid sessionId, params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new("actor_type", "Clinic"),
            new("user_id", userId.ToString()),
            new("clinic_id", clinicId.ToString()),
            new("session_id", sessionId.ToString())
        };
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:SigningKey"]!));
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"], audience: config["Jwt:Audience"],
            claims: claims, expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
