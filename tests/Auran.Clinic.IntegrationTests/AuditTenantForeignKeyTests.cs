using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DomainClinic = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.IntegrationTests;

public sealed class AuditTenantForeignKeyTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task AuditLog_DatabaseConstraint_RejectsCrossClinicActor()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        var clinicA = await CreateClinicAsync(dbContext, "AUDIT-A");
        var clinicB = await CreateClinicAsync(dbContext, "AUDIT-B");
        var foreignActor = await CreateUserAsync(dbContext, userManager, clinicA.Id, "foreign-audit-actor");
        var localActor = await CreateUserAsync(dbContext, userManager, clinicB.Id, "local-audit-actor");

        var validRows = await InsertAuditLogAsync(dbContext, clinicB.Id, localActor.Id, "ValidAuditAction");
        Assert.Equal(1, validRows);

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            InsertAuditLogAsync(dbContext, clinicB.Id, foreignActor.Id, "CrossClinicAuditAction"));

        Assert.Equal(547, exception.Number);
        Assert.Contains("FK_AuditLogs_Users_ActorUserId_ClinicId", exception.Message, StringComparison.Ordinal);
    }

    private static Task<int> InsertAuditLogAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid actorUserId,
        string action)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [AuditLogs]
                ([Id], [ClinicId], [ActorUserId], [Action], [EntityType], [OccurredAtUtc], [CreatedDate])
            VALUES ({id}, {clinicId}, {actorUserId}, {action}, {"TenantIntegrityTest"}, {now}, {now})
            """);
    }

    private static async Task<DomainClinic> CreateClinicAsync(AuranClinicDbContext dbContext, string label)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var clinic = new DomainClinic
        {
            Id = Guid.NewGuid(),
            Name = $"Audit Tenant {label} {suffix}",
            Code = $"AT-{label}-{suffix}",
            CreatedDate = DateTime.UtcNow
        };

        dbContext.Clinics.Add(clinic);
        await dbContext.SaveChangesAsync();
        return clinic;
    }

    private static async Task<User> CreateUserAsync(
        AuranClinicDbContext dbContext,
        UserManager<ApplicationIdentityUser> userManager,
        Guid clinicId,
        string label)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var email = $"{label}-{suffix}@auran.local";
        var identityUser = new ApplicationIdentityUser
        {
            UserName = email,
            Email = email,
            LockoutEnabled = true
        };

        var identityResult = await userManager.CreateAsync(identityUser, "ValidPassword1");
        Assert.True(
            identityResult.Succeeded,
            string.Join(", ", identityResult.Errors.Select(error => error.Description)));

        var user = new User
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            IdentityUserId = identityUser.Id,
            FullName = $"Audit Tenant {label}",
            Email = email,
            CreatedDate = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }
}
