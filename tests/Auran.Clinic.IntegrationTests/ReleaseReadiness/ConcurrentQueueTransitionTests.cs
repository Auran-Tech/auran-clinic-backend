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

public sealed class ConcurrentQueueTransitionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ConcurrentQueueTransitionTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Concurrent_terminal_moves_create_only_one_history_event_and_one_exit()
    {
        var clinicId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var visitId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var waitingId = Guid.NewGuid();
        var exitedId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var identityId = Guid.NewGuid().ToString("N");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
            db.Clinics.Add(new Auran.Clinic.Domain.Entities.Clinic
            {
                Id = clinicId, Name = "Concurrent queue clinic",
                Code = "C" + clinicId.ToString("N")[..12]
            });
            db.Set<ApplicationIdentityUser>().Add(new ApplicationIdentityUser
            {
                Id = identityId,
                UserName = "session-" + identityId,
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
            db.Visits.Add(new Visit
            {
                Id = visitId, ClinicId = clinicId, PatientId = patientId,
                DoctorId = doctorId, Status = Auran.Clinic.Domain.Enums.VisitStatus.Open,
                EntryAtUtc = DateTime.UtcNow
            });
            db.WorkflowStatuses.Add(new WorkflowStatus
            {
                Id = waitingId, ClinicId = clinicId,
                Code = "WAITING", Name = "Waiting", Color = "#123456",
                SortOrder = 0, IsSystemFinal = false
            });
            db.WorkflowStatuses.Add(new WorkflowStatus
            {
                Id = exitedId, ClinicId = clinicId,
                Code = "EXIT", Name = "Exit", Color = "#123456",
                SortOrder = 1, IsSystemFinal = true
            });
            db.WorkflowTransitions.Add(new WorkflowTransition
            {
                Id = Guid.NewGuid(), ClinicId = clinicId,
                FromStatusId = waitingId, ToStatusId = exitedId
            });
            db.QueueEntries.Add(new QueueEntry
            {
                Id = entryId, ClinicId = clinicId, PatientId = patientId,
                VisitId = visitId, DoctorId = doctorId,
                WorkflowStatusId = waitingId, EntryAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        var token = CreateToken(configuration, doctorId, clinicId, sessionId);
        using var clientA = _factory.CreateClient();
        using var clientB = _factory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new { queueEntryId = entryId, toWorkflowStatusId = exitedId };
        var responses = await Task.WhenAll(
            clientA.PostAsJsonAsync("/api/queue/move", payload),
            clientB.PostAsJsonAsync("/api/queue/move", payload));

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));

        using var verifyScope = _factory.Services.CreateScope();
        var verify = verifyScope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var entry = await verify.QueueEntries.AsNoTracking()
            .SingleAsync(queue => queue.Id == entryId);
        Assert.Equal(exitedId, entry.WorkflowStatusId);
        Assert.NotNull(entry.ExitAtUtc);
        Assert.Equal(1, await verify.QueueStatusHistory.AsNoTracking()
            .CountAsync(history => history.QueueEntryId == entryId));
        Assert.Equal(1, await verify.AuditLogs.AsNoTracking()
            .CountAsync(audit => audit.ClinicId == clinicId &&
                audit.Action == "Queue.StatusChanged" &&
                audit.EntityId == entryId.ToString()));

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
            new Claim("permission", "Queue_Move")
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
