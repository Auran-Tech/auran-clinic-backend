using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.Infrastructure.Platform;

public sealed class PlatformBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<PlatformBootstrapOptions> options) : IHostedService
{
    private readonly PlatformBootstrapOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
            return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        if (await dbContext.PlatformUsers.AnyAsync(cancellationToken))
            return;

        ValidateConfiguration();
        var email = _options.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(email) is not null)
            throw new InvalidOperationException("Platform bootstrap email is already used by another Identity account.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var identityUser = new ApplicationIdentityUser
        {
            UserName = email,
            Email = email,
            PhoneNumber = Clean(_options.Phone),
            EmailConfirmed = true,
            AccountType = AccountType.Platform
        };
        var result = await userManager.CreateAsync(identityUser, _options.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Platform bootstrap failed: " + string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        dbContext.PlatformUsers.Add(new PlatformUser
        {
            Id = Guid.NewGuid(),
            IdentityUserId = identityUser.Id,
            FullName = _options.FullName.Trim(),
            Email = email,
            Phone = Clean(_options.Phone),
            IsActive = true
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void ValidateConfiguration()
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
