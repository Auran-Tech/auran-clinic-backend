using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Infrastructure;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.UnitTests;

public sealed class AuthLockoutTests
{
    [Fact]
    public void Infrastructure_ConfiguresIdentityFailedLoginLockout()
    {
        using var provider = BuildProvider();
        var options = provider.GetRequiredService<IOptions<IdentityOptions>>().Value;

        Assert.True(options.Lockout.AllowedForNewUsers);
        Assert.Equal(5, options.Lockout.MaxFailedAccessAttempts);
        Assert.Equal(TimeSpan.FromMinutes(15), options.Lockout.DefaultLockoutTimeSpan);
    }

    [Fact]
    public async Task LoginAsync_LocksIdentityUserAfterFiveFailedPasswordAttempts()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        const string email = "lockout-test@auran.local";

        var identityUser = new ApplicationIdentityUser
        {
            UserName = email,
            Email = email,
            LockoutEnabled = true
        };
        var createResult = await userManager.CreateAsync(identityUser, "ValidPassword1");
        Assert.True(createResult.Succeeded, string.Join(", ", createResult.Errors.Select(error => error.Description)));

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var result = await authService.LoginAsync(new LoginRequest
            {
                Email = email,
                Password = "WrongPassword1"
            });

            Assert.Null(result);
        }

        var reloadedUser = await userManager.FindByEmailAsync(email);
        Assert.NotNull(reloadedUser);
        Assert.True(await userManager.IsLockedOutAsync(reloadedUser));
        Assert.True(reloadedUser.LockoutEnd > DateTimeOffset.UtcNow);
    }

    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=AuranClinicUnitTests;User Id=sa;Password=Unit_Test_Only_123!;TrustServerCertificate=True;Encrypt=False",
                ["Jwt:Issuer"] = "Auran.Clinic.Tests",
                ["Jwt:Audience"] = "Auran.Clinic.Tests.Client",
                ["Jwt:SigningKey"] = "Auran_Unit_Test_Signing_Key_At_Least_32_Bytes_Long",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "7"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);

        services.RemoveAll<DbContextOptions<AuranClinicDbContext>>();
        services.RemoveAll<IDbContextOptionsConfiguration<AuranClinicDbContext>>();
        services.AddDbContext<AuranClinicDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        return services.BuildServiceProvider();
    }
}
