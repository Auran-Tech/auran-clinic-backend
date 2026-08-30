using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Auran.Clinic.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly Guid ClinicAId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ClinicBId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid SuperUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid NormalUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public const string SuperEmail = "super@clinic.test";
    public const string NormalEmail = "normal@clinic.test";
    public const string Password = "Password123";

    public async Task InitializeAsync()
    {
        _ = CreateClient();
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        dbContext.Clinics.AddRange(
            new Auran.Clinic.Domain.Entities.Clinic
            {
                Id = ClinicAId,
                Name = "Clinic A",
                Code = "CLINIC_A",
                IsActive = true,
                PatientNumberPrefix = "AU"
            },
            new Auran.Clinic.Domain.Entities.Clinic
            {
                Id = ClinicBId,
                Name = "Clinic B",
                Code = "CLINIC_B",
                IsActive = true,
                PatientNumberPrefix = "BU"
            });
        await dbContext.SaveChangesAsync();

        var superIdentity = new ApplicationIdentityUser
        {
            Id = "identity-super",
            UserName = SuperEmail,
            Email = SuperEmail,
            EmailConfirmed = true
        };
        var normalIdentity = new ApplicationIdentityUser
        {
            Id = "identity-normal",
            UserName = NormalEmail,
            Email = NormalEmail,
            EmailConfirmed = true
        };

        Assert.True((await userManager.CreateAsync(superIdentity, Password)).Succeeded);
        Assert.True((await userManager.CreateAsync(normalIdentity, Password)).Succeeded);

        dbContext.Users.AddRange(
            new User
            {
                Id = SuperUserId,
                ClinicId = ClinicAId,
                IdentityUserId = superIdentity.Id,
                FullName = "Clinic Super User",
                Email = SuperEmail,
                IsActive = true,
                IsSuperUser = true
            },
            new User
            {
                Id = NormalUserId,
                ClinicId = ClinicAId,
                IdentityUserId = normalIdentity.Id,
                FullName = "Normal User",
                Email = NormalEmail,
                IsActive = true,
                IsSuperUser = false
            });

        dbContext.Patients.AddRange(
            new Patient
            {
                ClinicId = ClinicAId,
                PatientNumber = "AU-2026-1",
                FullName = "Clinic A Patient",
                Phone = "01000000001"
            },
            new Patient
            {
                ClinicId = ClinicBId,
                PatientNumber = "BU-2026-1",
                FullName = "Clinic B Patient",
                Phone = "01000000002"
            });

        await dbContext.SaveChangesAsync();
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    public async Task SetClinicActiveAsync(Guid clinicId, bool isActive)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var clinic = await dbContext.Clinics.SingleAsync(x => x.Id == clinicId);
        clinic.IsActive = isActive;
        await dbContext.SaveChangesAsync();
    }

    public async Task SetUserActiveAsync(Guid userId, bool isActive)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var user = await dbContext.Users.IgnoreQueryFilters().SingleAsync(x => x.Id == userId);
        user.IsActive = isActive;
        await dbContext.SaveChangesAsync();
    }
}
