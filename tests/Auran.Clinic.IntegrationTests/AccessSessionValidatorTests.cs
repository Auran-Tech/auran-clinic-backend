using System.Security.Claims;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Authentication;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.IntegrationTests;

public sealed class AccessSessionValidatorTests
{
    [Fact]
    public async Task PlatformSession_IsRejectedAfterRevocation()
    {
        await using var dbContext = CreateDbContext();
        var sessionId = Guid.NewGuid();
        var platformUserId = Guid.NewGuid();

        var session = new PlatformRefreshToken
        {
            Id = sessionId,
            PlatformUserId = platformUserId,
            TokenHash = "PLATFORM_SESSION_HASH",
            ExpiresDate = DateTime.UtcNow.AddDays(1),
            CreatedDate = DateTime.UtcNow
        };

        dbContext.PlatformRefreshTokens.Add(session);
        await dbContext.SaveChangesAsync();

        var principal = CreatePrincipal(
            ActorType.Platform,
            sessionId,
            ("platform_user_id", platformUserId.ToString()));
        var validator = new AccessSessionValidator(dbContext);

        Assert.True(await validator.IsActiveAsync(principal));

        session.RevokedDate = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        Assert.False(await validator.IsActiveAsync(principal));
    }

    [Fact]
    public async Task ClinicSession_IsRejectedAfterRevocation()
    {
        await using var dbContext = CreateDbContext();
        var sessionId = Guid.NewGuid();
        var clinicUserId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();

        var session = new RefreshToken
        {
            Id = sessionId,
            ClinicId = clinicId,
            UserId = clinicUserId,
            TokenHash = "CLINIC_SESSION_HASH",
            ExpiresDate = DateTime.UtcNow.AddDays(1),
            CreatedDate = DateTime.UtcNow
        };

        dbContext.RefreshTokens.Add(session);
        await dbContext.SaveChangesAsync();

        var principal = CreatePrincipal(
            ActorType.Clinic,
            sessionId,
            ("clinic_user_id", clinicUserId.ToString()),
            ("clinic_id", clinicId.ToString()));
        var validator = new AccessSessionValidator(dbContext);

        Assert.True(await validator.IsActiveAsync(principal));

        session.RevokedDate = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        Assert.False(await validator.IsActiveAsync(principal));
    }

    [Fact]
    public async Task Session_IsRejectedWhenActorClaimsDoNotMatchPersistedSession()
    {
        await using var dbContext = CreateDbContext();
        var sessionId = Guid.NewGuid();

        dbContext.PlatformRefreshTokens.Add(new PlatformRefreshToken
        {
            Id = sessionId,
            PlatformUserId = Guid.NewGuid(),
            TokenHash = "MISMATCH_SESSION_HASH",
            ExpiresDate = DateTime.UtcNow.AddDays(1),
            CreatedDate = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var principal = CreatePrincipal(
            ActorType.Platform,
            sessionId,
            ("platform_user_id", Guid.NewGuid().ToString()));
        var validator = new AccessSessionValidator(dbContext);

        Assert.False(await validator.IsActiveAsync(principal));
    }

    private static AuranClinicDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuranClinicDbContext>()
            .UseInMemoryDatabase($"access-session-tests-{Guid.NewGuid():N}")
            .Options;

        return new AuranClinicDbContext(options);
    }

    private static ClaimsPrincipal CreatePrincipal(
        ActorType actorType,
        Guid sessionId,
        params (string Type, string Value)[] actorClaims)
    {
        var claims = new List<Claim>
        {
            new("actor_type", actorType.ToString()),
            new("session_id", sessionId.ToString())
        };
        claims.AddRange(actorClaims.Select(x => new Claim(x.Type, x.Value)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }
}
