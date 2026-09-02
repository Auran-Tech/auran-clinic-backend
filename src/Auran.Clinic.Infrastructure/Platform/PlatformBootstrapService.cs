using System.Data;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.Infrastructure.Platform;

public sealed class PlatformBootstrapService(
    AuranClinicDbContext dbContext,
    UserManager<ApplicationIdentityUser> userManager,
    IOptions<PlatformBootstrapOptions> options)
{
    private const string BootstrapLockResource = "Auran.Platform.FirstAdminBootstrap";
    private readonly PlatformBootstrapOptions _options = options.Value;

    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return;

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            try
            {
                await AcquireBootstrapLockAsync(cancellationToken);

                if (await dbContext.PlatformUsers.AnyAsync(cancellationToken))
                {
                    await transaction.CommitAsync(cancellationToken);
                    return;
                }

                ValidateInitialAdminConfiguration();
                var email = _options.Email.Trim().ToLowerInvariant();
                var existingIdentity = await userManager.FindByEmailAsync(email);
                if (existingIdentity is not null)
                {
                    throw new InvalidOperationException(
                        "Platform bootstrap email is already used by another Identity account.");
                }

                var identityUser = new ApplicationIdentityUser
                {
                    UserName = email,
                    Email = email,
                    PhoneNumber = Clean(_options.Phone),
                    EmailConfirmed = true,
                    LockoutEnabled = true,
                    AccountType = AccountType.Platform
                };

                var identityResult = await userManager.CreateAsync(identityUser, _options.Password);
                if (!identityResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Platform Admin bootstrap failed: " +
                        string.Join("; ", identityResult.Errors.Select(error => error.Description)));
                }

                dbContext.PlatformUsers.Add(new PlatformUser
                {
                    Id = Guid.NewGuid(),
                    IdentityUserId = identityUser.Id,
                    FullName = _options.FullName.Trim(),
                    Email = email,
                    Phone = Clean(_options.Phone),
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                });

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                dbContext.ChangeTracker.Clear();
                throw;
            }
        });
    }

    private async Task AcquireBootstrapLockAsync(CancellationToken cancellationToken)
    {
        var transaction = dbContext.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Platform bootstrap requires an active database transaction.");

        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = @resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 15000;
            SELECT @result;
            """;

        var resourceParameter = command.CreateParameter();
        resourceParameter.ParameterName = "@resource";
        resourceParameter.Value = BootstrapLockResource;
        command.Parameters.Add(resourceParameter);

        var result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (result < 0)
        {
            throw new InvalidOperationException(
                $"Could not acquire the platform bootstrap database lock. SQL Server result: {result}.");
        }
    }

    private void ValidateInitialAdminConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.FullName) ||
            string.IsNullOrWhiteSpace(_options.Email) ||
            string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException(
                "PlatformBootstrap FullName, Email and Password are required when bootstrap is enabled and no Platform user exists.");
        }
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
