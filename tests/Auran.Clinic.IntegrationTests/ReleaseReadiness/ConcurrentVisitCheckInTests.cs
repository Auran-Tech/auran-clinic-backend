using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Auran.Clinic.IntegrationTests.ReleaseReadiness;

public sealed class ConcurrentVisitCheckInTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ConcurrentVisitCheckInTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Concurrent_check_ins_allow_only_one_open_visit_and_queue_entry()
    {
        var clinicId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var identityId = Guid.NewGuid().ToString("N");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
            db.Clinics.Add(new Auran.Clinic.Domain.Entities.Clinic
            {
                Id = clinicId, Name = "Concurrent check-in clinic",
                Code = "C" + clinicId.ToString("N")[..12]
            });
            db.Set<ApplicationIdentityUser>().Add(new ApplicationIdentityUser
            {
                Id = identityId,
                UserName = "checkin-" + identityId,
                NormalizedUserName = "CHECKIN-" + identityId.ToUpperInvariant(),
                Email = identityId + "@example.invalid",
                NormalizedEmail = identityId.ToUpperInvariant() + "@EXAMPLE.INVALID",
                SecurityStamp = Guid.NewGuid().ToString()
            });
            db.Users.Add(new User
            {
                Id = doctorId, ClinicId = clinicId, IdentityUserId = identityId,
                FullName = "Test doctor", IsActive = true
            });
            db.RefreshTokens.Add(new RefreshToken
            {
                Id = sessionId, ClinicId = clinicId, UserId = doctorId,
                TokenHash = Guid.NewGuid().ToString("N"),
                ExpiresDate = DateTime.UtcNow.AddHours(1)
            });
            db.Patients.Add(new Patient
            {
                Id = patientId, ClinicId = clinicId,
                PatientNumber = "P" + patientId.ToString("N")[..12],
                FullName = "Check-in patient", Phone = patientId.ToString("N")
            });
            db.WorkflowStatuses.Add(new WorkflowStatus
            {
                Id = Guid.NewGuid(), ClinicId = clinicId,
                Code = "WAITING", Name = "Waiting", Color = "#123456",
                SortOrder = 0, IsSystemFinal = false
            });
            await db.SaveChangesAsync();
        }

        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        var token = CreateToken(configuration, doctorId, clinicId, sessionId);
        using var clientA = _factory.CreateClient();
        using var clientB = _factory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new { patientId, doctorId };
        var responses = await Task.WhenAll(
            clientA.PostAsJsonAsync("/api/visits/start", payload),
            clientB.PostAsJsonAsync("/api/visits/start", payload));

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));

        using var verifyScope = _factory.Services.CreateScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var visits = await verify.Visits.AsNoTracking()
            .Where(visit => visit.ClinicId == clinicId && visit.PatientId == patientId)
            .Select(visit => visit.Id)
            .ToListAsync();
        Assert.Single(visits);
        Assert.Equal(1, await verify.QueueEntries.AsNoTracking()
            .CountAsync(entry => entry.ClinicId == clinicId && entry.VisitId == visits[0]));
        Assert.Equal(1, await verify.QueueStatusHistory.AsNoTracking()
            .CountAsync(history => history.ClinicId == clinicId &&
                verify.QueueEntries.Any(entry => entry.Id == history.QueueEntryId && entry.VisitId == visits[0])));

        foreach (var response in responses) response.Dispose();
    }

    private static string CreateToken(
        IConfiguration configuration, Guid userId, Guid clinicId, Guid sessionId)
    {
        var claims = new[]
        {
            new Claim("actor_type", "Clinic"),
            new Claim("user_id", userId.ToString()),
            new Claim("clinic_id", clinicId.ToString()),
            new Claim("session_id", sessionId.ToString()),
            new Claim("permission", "Visit_Start")
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SigningKey"]!));
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
