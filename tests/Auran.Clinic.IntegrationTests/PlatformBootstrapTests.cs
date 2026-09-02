using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Auran.Clinic.Infrastructure.Platform;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.IntegrationTests;

public sealed class PlatformBootstrapTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task BootstrapAsync_ConcurrentFirstAdminAttempts_CreateExactlyOnePlatformUser()
    {
        await using var database = await BootstrapTestDatabase.CreateAsync(factory);
        var email = $"platform-{Guid.NewGuid():N}@example.test";
        var options = new PlatformBootstrapOptions
        {
            Enabled = true,
            FullName = "AURAN Platform Admin",
            Email = email,
            Password = "StrongPass1",
            Phone = "+201000000000"
        };

        await Task.WhenAll(
            database.RunBootstrapAsync(options),
            database.RunBootstrapAsync(options));

        await database.VerifyAsync(async (dbContext, userManager) =>
        {
            var platformUsers = await dbContext.PlatformUsers.AsNoTracking().ToListAsync();
            Assert.Single(platformUsers);
            Assert.Equal(email, platformUsers[0].Email);
            Assert.True(platformUsers[0].IsActive);

            var identityUser = await userManager.FindByEmailAsync(email);
            Assert.NotNull(identityUser);
            Assert.Equal(AccountType.Platform, identityUser.AccountType);
            Assert.True(await userManager.CheckPasswordAsync(identityUser, options.Password));
        });

        await database.RunBootstrapAsync(new PlatformBootstrapOptions
        {
            Enabled = true
        });
    }

    [Fact]
    public async Task BootstrapAsync_EmailAlreadyOwnedByClinicIdentity_RejectsWithoutChangingIdentityType()
    {
        await using var database = await BootstrapTestDatabase.CreateAsync(factory);
        var email = $"clinic-{Guid.NewGuid():N}@example.test";

        await database.CreateIdentityAsync(new ApplicationIdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            LockoutEnabled = true,
            AccountType = AccountType.Clinic
        }, "StrongPass1");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            database.RunBootstrapAsync(new PlatformBootstrapOptions
            {
                Enabled = true,
                FullName = "AURAN Platform Admin",
                Email = email,
                Password = "StrongPass2"
            }));

        Assert.Contains("already used", exception.Message, StringComparison.OrdinalIgnoreCase);

        await database.VerifyAsync(async (dbContext, userManager) =>
        {
            Assert.Empty(await dbContext.PlatformUsers.AsNoTracking().ToListAsync());
            var identityUser = await userManager.FindByEmailAsync(email);
            Assert.NotNull(identityUser);
            Assert.Equal(AccountType.Clinic, identityUser.AccountType);
        });
    }

    private sealed class BootstrapTestDatabase(
        ServiceProvider serviceProvider,
        string connectionString) : IAsyncDisposable
    {
        public static async Task<BootstrapTestDatabase> CreateAsync(ApiFactory factory)
        {
            var configuration = factory.Services.GetRequiredService<IConfiguration>();
            var sourceConnectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is required for bootstrap integration tests.");

            var connectionBuilder = new SqlConnectionStringBuilder(sourceConnectionString)
            {
                InitialCatalog = $"AuranBootstrapTests_{Guid.NewGuid():N}"
            };

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICurrentUserContext, AnonymousCurrentUserContext>();
            services.AddDbContext<AuranClinicDbContext>(options =>
                options.UseSqlServer(connectionBuilder.ConnectionString));
            services.AddIdentityCore<ApplicationIdentityUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                })
                .AddEntityFrameworkStores<AuranClinicDbContext>();

            var provider = services.BuildServiceProvider();
            var database = new BootstrapTestDatabase(provider, connectionBuilder.ConnectionString);

            await using var scope = provider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
            await dbContext.Database.MigrateAsync();

            return database;
        }

        public async Task RunBootstrapAsync(PlatformBootstrapOptions options)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();
            var service = new PlatformBootstrapService(
                dbContext,
                userManager,
                Options.Create(options));

            await service.BootstrapAsync();
        }

        public async Task CreateIdentityAsync(ApplicationIdentityUser identityUser, string password)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();
            var result = await userManager.CreateAsync(identityUser, password);
            Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Description)));
        }

        public async Task VerifyAsync(
            Func<AuranClinicDbContext, UserManager<ApplicationIdentityUser>, Task> assertion)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();
            await assertion(dbContext, userManager);
        }

        public async ValueTask DisposeAsync()
        {
            await using (var scope = serviceProvider.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
                await dbContext.Database.EnsureDeletedAsync();
            }

            await serviceProvider.DisposeAsync();
        }
    }

    private sealed class AnonymousCurrentUserContext : ICurrentUserContext
    {
        public bool IsAuthenticated => false;
        public Guid? UserId => null;
        public Guid? ClinicId => null;
        public bool IsSuperUser => false;
    }
}
