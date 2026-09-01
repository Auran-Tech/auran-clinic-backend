using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DomainClinic = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.IntegrationTests;

public sealed class AuthTenantForeignKeyTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task UserRole_DatabaseConstraint_AllowsSameClinicAndRejectsCrossClinicUser()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        var clinicA = await CreateClinicAsync(dbContext, "UR-A");
        var clinicB = await CreateClinicAsync(dbContext, "UR-B");
        var user = await CreateUserAsync(dbContext, userManager, clinicA.Id, "tenant-user-role");
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Code = $"TENANT_ROLE_{Guid.NewGuid():N}",
            Name = "Tenant FK Test Role"
        };
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();

        var validAssignmentId = Guid.NewGuid();
        var validCreatedDate = DateTime.UtcNow;
        var validRows = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [UserRoles] ([Id], [ClinicId], [UserId], [RoleId], [CreatedDate])
            VALUES ({validAssignmentId}, {clinicA.Id}, {user.Id}, {role.Id}, {validCreatedDate})
            """);
        Assert.Equal(1, validRows);

        var invalidAssignmentId = Guid.NewGuid();
        var invalidCreatedDate = DateTime.UtcNow;
        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO [UserRoles] ([Id], [ClinicId], [UserId], [RoleId], [CreatedDate])
                VALUES ({invalidAssignmentId}, {clinicB.Id}, {user.Id}, {role.Id}, {invalidCreatedDate})
                """));

        Assert.Equal(547, exception.Number);
        Assert.Contains("FK_UserRoles_Users_UserId_ClinicId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RefreshToken_DatabaseConstraint_AllowsSameClinicAndRejectsCrossClinicUser()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        var clinicA = await CreateClinicAsync(dbContext, "RT-A");
        var clinicB = await CreateClinicAsync(dbContext, "RT-B");
        var user = await CreateUserAsync(dbContext, userManager, clinicA.Id, "tenant-refresh-token");

        var validTokenId = Guid.NewGuid();
        var validTokenHash = Guid.NewGuid().ToString("N");
        var validExpiry = DateTime.UtcNow.AddHours(1);
        var validCreatedDate = DateTime.UtcNow;
        var validRows = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [RefreshTokens] ([Id], [ClinicId], [UserId], [TokenHash], [ExpiresDate], [CreatedDate])
            VALUES ({validTokenId}, {clinicA.Id}, {user.Id}, {validTokenHash}, {validExpiry}, {validCreatedDate})
            """);
        Assert.Equal(1, validRows);

        var invalidTokenId = Guid.NewGuid();
        var invalidTokenHash = Guid.NewGuid().ToString("N");
        var invalidExpiry = DateTime.UtcNow.AddHours(1);
        var invalidCreatedDate = DateTime.UtcNow;
        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO [RefreshTokens] ([Id], [ClinicId], [UserId], [TokenHash], [ExpiresDate], [CreatedDate])
                VALUES ({invalidTokenId}, {clinicB.Id}, {user.Id}, {invalidTokenHash}, {invalidExpiry}, {invalidCreatedDate})
                """));

        Assert.Equal(547, exception.Number);
        Assert.Contains("FK_RefreshTokens_Users_UserId_ClinicId", exception.Message, StringComparison.Ordinal);
    }

    private static async Task<DomainClinic> CreateClinicAsync(AuranClinicDbContext dbContext, string label)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var clinic = new DomainClinic
        {
            Id = Guid.NewGuid(),
            Name = $"Tenant FK {label} {suffix}",
            Code = $"TF-{label}-{suffix}"
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
            FullName = $"Tenant FK {label}",
            Email = email
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }
}
