using System.Data;
using Auran.Clinic.Application.Files;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.Infrastructure.Files;

public sealed class FileDeletionOutboxOptions
{
    public const string SectionName = "FileDeletionOutbox";
    public bool Enabled { get; init; } = true;
    public int PollIntervalSeconds { get; init; } = 30;
}

/// <summary>
/// Durable SQL-backed local-file cleanup. Each claim is a single atomic UPDATE
/// with a lease: parallel API nodes cannot process a job simultaneously.
/// Retrying a completed delete is safe because IFileStorageService.DeleteAsync
/// is idempotent if the blob no longer exists.
/// </summary>
public sealed class FileDeletionOutboxWorker(
    IServiceScopeFactory scopes,
    IOptions<FileDeletionOutboxOptions> options,
    ILogger<FileDeletionOutboxWorker> logger) : BackgroundService
{
    private readonly FileDeletionOutboxOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            return;

        var interval = TimeSpan.FromSeconds(Math.Clamp(_options.PollIntervalSeconds, 10, 3600));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Durable file deletion outbox cycle failed.");
            }

            try { await Task.Delay(interval, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default)
    {
        var processed = 0;
        // Bounded batches prevent unbounded startup work and allow other hosts to poll.
        for (var i = 0; i < 25; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
            var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

            var job = await ClaimAsync(db, cancellationToken);
            if (job is null)
                break;

            try
            {
                // Fail closed: never delete bytes that are still registered for ANY clinic.
                var referenced = await db.Files.IgnoreQueryFilters().AsNoTracking()
                    .AnyAsync(f => f.StorageKey == job.Value.Key &&
                        f.StorageProvider == job.Value.Provider, cancellationToken);
                if (referenced)
                {
                    await RescheduleAsync(db, job.Value.Id, job.Value.Attempts, 60,
                        "Storage key remains referenced in Files.", cancellationToken);
                    continue;
                }

                if (!string.Equals(job.Value.Provider, "Local", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Unsupported file storage provider.");

                await storage.DeleteAsync(job.Value.Key, cancellationToken);
                if (await MarkCompleteAsync(db, job.Value.Id, job.Value.Attempts, cancellationToken))
                    processed++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var retryMinutes = Math.Min(360, 1 << Math.Min(job.Value.Attempts, 8));
                await RescheduleAsync(db, job.Value.Id, job.Value.Attempts, retryMinutes,
                    ex.GetType().Name, cancellationToken);
                logger.LogWarning("File deletion outbox retry scheduled after attempt {Attempt}.",
                    job.Value.Attempts);
            }
        }
        return processed;
    }

    private static async Task<(Guid Id, string Key, string Provider, int Attempts)?> ClaimAsync(
        AuranClinicDbContext db, CancellationToken ct)
    {
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var cmd = db.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = """
                ;WITH next_job AS (
                    SELECT TOP (1) * FROM dbo.FileDeletionOutbox WITH (ROWLOCK, READPAST, UPDLOCK, READCOMMITTEDLOCK)
                    WHERE CompletedAtUtc IS NULL
                      AND NotBeforeUtc <= SYSUTCDATETIME()
                      AND (LeaseUntilUtc IS NULL OR LeaseUntilUtc < SYSUTCDATETIME())
                    ORDER BY NotBeforeUtc, CreatedAtUtc
                )
                UPDATE next_job
                SET LeaseUntilUtc = DATEADD(minute, 2, SYSUTCDATETIME()),
                    AttemptCount = AttemptCount + 1
                OUTPUT INSERTED.Id, INSERTED.StorageKey,
                       INSERTED.StorageProvider, INSERTED.AttemptCount;
                """;
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return null;
            return (reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3));
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }

    // Only the current lease owner can finalize/reschedule a job. A worker
    // that exceeded its lease cannot overwrite a newer worker's claim.
    private static async Task<bool> MarkCompleteAsync(
        AuranClinicDbContext db, Guid id, int attemptCount, CancellationToken ct)
    {
        var affected = await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE dbo.FileDeletionOutbox
            SET CompletedAtUtc = SYSUTCDATETIME(), LeaseUntilUtc = NULL, LastError = NULL
            WHERE Id = {id} AND CompletedAtUtc IS NULL
                AND AttemptCount = {attemptCount}
                AND LeaseUntilUtc >= SYSUTCDATETIME()
            """, ct);
        return affected == 1;
    }

    private static async Task RescheduleAsync(
        AuranClinicDbContext db, Guid id, int attemptCount,
        int delayMinutes, string error, CancellationToken ct)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE dbo.FileDeletionOutbox
            SET LeaseUntilUtc = NULL,
                NotBeforeUtc = DATEADD(minute, {delayMinutes}, SYSUTCDATETIME()),
                LastError = {error}
            WHERE Id = {id} AND CompletedAtUtc IS NULL
                AND AttemptCount = {attemptCount}
                AND LeaseUntilUtc >= SYSUTCDATETIME()
            """, ct);
    }
}
