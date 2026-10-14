using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.Infrastructure.Files;

/// <summary>
/// Opt-in local-storage recovery for crashes between SQL commit and blob cleanup.
/// It never deletes a file younger than the configured grace period, or one still
/// present in the Files table. Multiple instances may retry the same deletion.
/// </summary>
public sealed class LocalOrphanFileReconciliationService(
    LocalFileStorageService storage,
    IServiceScopeFactory scopes,
    IOptions<FileStorageOptions> options,
    ILogger<LocalOrphanFileReconciliationService> logger) : BackgroundService
{
    private readonly FileStorageOptions _settings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.OrphanCleanupEnabled)
            return;

        // Avoid accidental aggressive deletion caused by misconfigured values.
        if (_settings.OrphanGracePeriodHours < 24 ||
            _settings.OrphanCleanupIntervalMinutes < 15)
        {
            throw new InvalidOperationException(
                "Local orphan cleanup requires at least a 24-hour grace and a 15-minute interval.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReconcileOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Local orphan file reconciliation failed; retry on next cycle.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromMinutes(_settings.OrphanCleanupIntervalMinutes),
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    public async Task<int> ReconcileOnceAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddHours(-Math.Max(24, _settings.OrphanGracePeriodHours));
        var candidates = storage.GetOrphanCandidates(cutoff);
        if (candidates.Count == 0)
            return 0;

        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var deleted = 0;

        foreach (var key in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Check immediately before physical removal; the DB is the authority
            // for all clinics, and a surviving FileRecord must never be deleted.
            var registered = await db.Files.AsNoTracking()
                .AnyAsync(file => file.StorageKey == key, cancellationToken);
            if (registered)
                continue;

            try
            {
                await storage.DeleteAsync(key, cancellationToken);
                deleted++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Orphan blob cleanup failed; will retry in a later cycle.");
            }
        }

        if (deleted != 0)
            logger.LogInformation("Removed {FileCount} unreferenced, aged local storage blobs.", deleted);

        return deleted;
    }
}
