using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
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
        foreach (var endpoint in new[] { "clinical-measurements", "clinical-order-attachments" })
        {
            var ownResponse = await client.GetAsync($"/api/{endpoint}?visitId={visitA}");
            var foreignResponse = await client.GetAsync($"/api/{endpoint}?visitId={visitB}");
            Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, foreignResponse.StatusCode);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Patient_View"));
        foreach (var endpoint in new[] { "clinical-measurements", "clinical-order-attachments" })
        {
            var deniedResponse = await client.GetAsync($"/api/{endpoint}?visitId={visitA}");
            Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);
        }

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
