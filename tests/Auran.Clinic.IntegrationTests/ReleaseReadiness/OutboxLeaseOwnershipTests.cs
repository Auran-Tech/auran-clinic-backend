using Auran.Clinic.Infrastructure.Files;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.IntegrationTests.ReleaseReadiness;

public sealed class OutboxLeaseOwnershipTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Parallel_workers_finish_one_durable_job_without_double_claiming()
    {
        var jobId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();
        var key = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}.pdf";
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO dbo.FileDeletionOutbox
                    (Id, ClinicId, StorageProvider, StorageKey, NotBeforeUtc)
                VALUES ({jobId}, {clinicId}, {"Local"}, {key}, SYSUTCDATETIME())
                """);
        }

        var scopes = factory.Services.GetRequiredService<IServiceScopeFactory>();
        var options = Options.Create(new FileDeletionOutboxOptions());
        var logger = NullLogger<FileDeletionOutboxWorker>.Instance;
        var first = new FileDeletionOutboxWorker(scopes, options, logger);
        var second = new FileDeletionOutboxWorker(scopes, options, logger);

        await Task.WhenAll(first.ProcessBatchAsync(), second.ProcessBatchAsync());

        await using var verification = factory.Services.CreateAsyncScope();
        var verifyDb = verification.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var completed = await verifyDb.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS Value FROM dbo.FileDeletionOutbox WHERE Id = {0} AND CompletedAtUtc IS NOT NULL",
            jobId).SingleAsync();
        Assert.Equal(1, completed);
        var attempts = await verifyDb.Database.SqlQueryRaw<int>(
            "SELECT AttemptCount AS Value FROM dbo.FileDeletionOutbox WHERE Id = {0}", jobId)
            .SingleAsync();
        Assert.Equal(1, attempts);
    }
}
