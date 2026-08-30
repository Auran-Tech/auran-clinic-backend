using System.Security.Claims;
using Auran.Clinic.Application.Authentication;
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

        var principal = CreatePrincipal(ActorType.Platform, sessionId, ("platform_user_id", platformUserId.ToString()));
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

        dbContext.Clinics.Add(new Auran.Clinic.Domain.Entities.Clinic
        {
            Id = clinicId,
            Name = "Clinic",
            Code = "CLINIC-1",
            IsActive = true
        });
        dbContext.Users.Add(new User
        {
            Id = clinicUserId,
            ClinicId = clinicId,
            IdentityUserId = Guid.NewGuid().ToString(),
            FullName = "Clinic User",
            IsActive = true
        });
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
    public async Task ClinicSession_IsRejectedWhenClinicOrUserIsDisabled()
    {
        await using var dbContext = CreateDbContext();
        var sessionId = Guid.NewGuid();
        var clinicUserId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();
        var clinic = new Auran.Clinic.Domain.Entities.Clinic
        {
            Id = clinicId,
            Name = "Clinic",
            Code = "CLINIC-2",
            IsActive = true
        };
        var user = new User
        {
            Id = clinicUserId,
            ClinicId = clinicId,
            IdentityUserId = Guid.NewGuid().ToString(),
            FullName = "Clinic User",
            IsActive = true
        };
        dbContext.AddRange(clinic, user, new RefreshToken
        {
            Id = sessionId,
            ClinicId = clinicId,
            UserId = clinicUserId,
            TokenHash = "ACTIVE_SESSION_HASH",
            ExpiresDate = DateTime.UtcNow.AddDays(1),
            CreatedDate = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var principal = CreatePrincipal(
            ActorType.Clinic,
            sessionId,
            ("clinic_user_id", clinicUserId.ToString()),
            ("clinic_id", clinicId.ToString()));
        var validator = new AccessSessionValidator(dbContext);
        Assert.True(await validator.IsActiveAsync(principal));

        user.IsActive = false;
        await dbContext.SaveChangesAsync();
        Assert.False(await validator.IsActiveAsync(principal));

        user.IsActive = true;
        clinic.IsActive = false;
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

        var principal = CreatePrincipal(ActorType.Platform, sessionId, ("platform_user_id", Guid.NewGuid().ToString()));
        Assert.False(await new AccessSessionValidator(dbContext).IsActiveAsync(principal));
    }

    private static AuranClinicDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuranClinicDbContext>()
            .UseInMemoryDatabase($"access-session-tests-{Guid.NewGuid():N}")
            .Options;
        return new AuranClinicDbContext(options, new TestActor());
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

    private sealed class TestActor : ICurrentActor
    {
        public bool IsAuthenticated => false;
        public ActorType ActorType => ActorType.System;
        public string? IdentityUserId => null;
        public Guid? PlatformUserId => null;
        public Guid? ClinicUserId => null;
        public Guid? ClinicId => null;
        public bool IsClinicSuperUser => false;
        public string? DisplayName => null;
        public string? Email => null;
    }
}
