using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DomainClinic = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.IntegrationTests;

public sealed class PlatformIdentityBoundaryTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task PlatformProfile_RejectsClinicIdentity()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();
        var identity = await CreateIdentityAsync(userManager, AccountType.Clinic, "clinic-for-platform");

        dbContext.PlatformUsers.Add(new PlatformUser
        {
            Id = Guid.NewGuid(),
            IdentityUserId = identity.Id,
            FullName = "Invalid Platform Profile",
            Email = $"invalid-platform-{Guid.NewGuid():N}@auran.local",
            CreatedDate = DateTime.UtcNow
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        AssertSqlForeignKeyViolation(exception);
    }

    [Fact]
    public async Task ClinicProfile_RejectsPlatformIdentity()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();
        var identity = await CreateIdentityAsync(userManager, AccountType.Platform, "platform-for-clinic");
        var clinic = await CreateClinicAsync(dbContext, "platform-identity");

        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            ClinicId = clinic.Id,
            IdentityUserId = identity.Id,
            FullName = "Invalid Clinic Profile",
            Email = $"invalid-clinic-{Guid.NewGuid():N}@auran.local",
            CreatedDate = DateTime.UtcNow
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        AssertSqlForeignKeyViolation(exception);
    }

    [Fact]
    public async Task PlatformProfile_AcceptsPlatformIdentity()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();
        var identity = await CreateIdentityAsync(userManager, AccountType.Platform, "valid-platform");
        var platformUser = new PlatformUser
        {
            Id = Guid.NewGuid(),
            IdentityUserId = identity.Id,
            FullName = "Valid Platform Profile",
            Email = $"valid-platform-{Guid.NewGuid():N}@auran.local",
            CreatedDate = DateTime.UtcNow
        };

        dbContext.PlatformUsers.Add(platformUser);
        await dbContext.SaveChangesAsync();

        Assert.True(await dbContext.PlatformUsers.AnyAsync(user => user.Id == platformUser.Id));
    }

    private static async Task<ApplicationIdentityUser> CreateIdentityAsync(
        UserManager<ApplicationIdentityUser> userManager,
        AccountType accountType,
        string label)
    {
        var email = $"{label}-{Guid.NewGuid():N}@auran.local";
        var identity = new ApplicationIdentityUser
        {
            UserName = email,
            Email = email,
            AccountType = accountType,
            LockoutEnabled = true
        };

        var result = await userManager.CreateAsync(identity, "ValidPassword1");
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(error => error.Description)));
        return identity;
    }

    private static async Task<DomainClinic> CreateClinicAsync(AuranClinicDbContext dbContext, string label)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var clinic = new DomainClinic
        {
            Id = Guid.NewGuid(),
            Name = $"Platform Boundary {label} {suffix}",
            Code = $"PB-{suffix}",
            CreatedDate = DateTime.UtcNow
        };

        dbContext.Clinics.Add(clinic);
        await dbContext.SaveChangesAsync();
        return clinic;
    }

    private static void AssertSqlForeignKeyViolation(DbUpdateException exception)
    {
        var sqlException = Assert.IsType<SqlException>(exception.InnerException);
        Assert.Equal(547, sqlException.Number);
    }
}
