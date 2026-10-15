using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Application.Files;
using Auran.Clinic.Infrastructure.Files;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.IntegrationTests.ReleaseReadiness;

public sealed class LocalOrphanReconciliationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Durable_outbox_never_removes_a_blob_still_registered_in_SQL()
    {
        var storage = factory.Services.GetRequiredService<IFileStorageService>();
        var jobId = Guid.NewGuid();
        string? storageKey = null;

        try
        {
            await using var input = new MemoryStream("registered data"u8.ToArray());
            var blob = await storage.SaveAsync(input, "registered.pdf", "application/pdf");
            storageKey = blob.StorageKey;

            var clinicId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var identityId = Guid.NewGuid().ToString("N");
            var fileId = Guid.NewGuid();

            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();

            db.Clinics.Add(new Auran.Clinic.Domain.Entities.Clinic
            {
                Id = clinicId, Name = "Protected file outbox test",
                Code = "OF" + clinicId.ToString("N")[..12]
            });
            db.Set<ApplicationIdentityUser>().Add(new ApplicationIdentityUser
            {
                Id = identityId,
                UserName = "outbox-" + identityId,
                NormalizedUserName = "OUTBOX-" + identityId.ToUpperInvariant(),
                Email = identityId + "@example.invalid",
                NormalizedEmail = identityId.ToUpperInvariant() + "@EXAMPLE.INVALID",
                SecurityStamp = Guid.NewGuid().ToString()
            });
            db.Users.Add(new User
            {
                Id = userId, ClinicId = clinicId, IdentityUserId = identityId,
                FullName = "Protected file test user", IsActive = true
            });
            db.Files.Add(new FileRecord
            {
                Id = fileId, ClinicId = clinicId,
                OriginalName = "registered.pdf", StoredName = blob.StoredName,
                StorageKey = blob.StorageKey, StorageProvider = "Local",
                ContentType = "application/pdf", Size = blob.Size,
                UploadedByUserId = userId, UploadedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO dbo.FileDeletionOutbox
                    (Id, ClinicId, StorageProvider, StorageKey, NotBeforeUtc)
                VALUES ({jobId}, {clinicId}, {"Local"}, {blob.StorageKey}, SYSUTCDATETIME())
                """);

            var worker = new FileDeletionOutboxWorker(
                factory.Services.GetRequiredService<IServiceScopeFactory>(),
                Options.Create(new FileDeletionOutboxOptions { Enabled = false }),
                NullLogger<FileDeletionOutboxWorker>.Instance);

            Assert.Equal(0, await worker.ProcessBatchAsync());
            await using (var retained = await storage.OpenReadAsync(blob.StorageKey))
                Assert.True(retained.CanRead);

            var deferred = await db.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*) AS Value FROM dbo.FileDeletionOutbox
                WHERE Id = @id AND CompletedAtUtc IS NULL
                    AND AttemptCount >= 1 AND LastError IS NOT NULL
                    AND NotBeforeUtc > SYSUTCDATETIME()
                """,
                new Microsoft.Data.SqlClient.SqlParameter("@id", jobId)).SingleAsync();
            Assert.Equal(1, deferred);

            db.Files.Remove((await db.Files.SingleAsync(file => file.Id == fileId)));
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE dbo.FileDeletionOutbox
                SET NotBeforeUtc = DATEADD(second, -1, SYSUTCDATETIME()),
                    LeaseUntilUtc = NULL
                WHERE Id = {jobId}
                """);

            Assert.Equal(1, await worker.ProcessBatchAsync());
            var completed = await db.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*) AS Value FROM dbo.FileDeletionOutbox
                WHERE Id = @id AND CompletedAtUtc IS NOT NULL
                """,
                new Microsoft.Data.SqlClient.SqlParameter("@id", jobId)).SingleAsync();
            Assert.Equal(1, completed);

            await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                await using var _ = await storage.OpenReadAsync(blob.StorageKey);
            });
        }
        finally
        {
            if (storageKey is not null)
                await storage.DeleteAsync(storageKey);
        }
    }

    [Fact]
    public async Task Durable_outbox_retries_failed_provider_and_eventually_deletes_file()
    {
        var storage = factory.Services.GetRequiredService<IFileStorageService>();
        var jobId = Guid.NewGuid();
        string? storedKey = null;
        try
        {
            await using var input = new MemoryStream("delayed clean-up"u8.ToArray());
            var blob = await storage.SaveAsync(input, "test.pdf", "application/pdf");
            storedKey = blob.StorageKey;

            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
            // Intentionally unrecognized provider: a worker must retain the job,
            // schedule a retry and not remove the existing file.
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO dbo.FileDeletionOutbox
                    (Id, ClinicId, StorageProvider, StorageKey, NotBeforeUtc)
                VALUES ({jobId}, {Guid.NewGuid()}, {"Unavailable"}, {blob.StorageKey}, SYSUTCDATETIME())
                """);

            var worker = new FileDeletionOutboxWorker(
                factory.Services.GetRequiredService<IServiceScopeFactory>(),
                Options.Create(new FileDeletionOutboxOptions { Enabled = false }),
                NullLogger<FileDeletionOutboxWorker>.Instance);

            await worker.ProcessBatchAsync();

            var failed = await db.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*) AS Value FROM dbo.FileDeletionOutbox WHERE Id = @id AND CompletedAtUtc IS NULL AND AttemptCount >= 1 AND LastError IS NOT NULL AND NotBeforeUtc > SYSUTCDATETIME()",
                new Microsoft.Data.SqlClient.SqlParameter("@id", jobId)).SingleAsync();
            Assert.Equal(1, failed);
            await using (var remaining = await storage.OpenReadAsync(blob.StorageKey))
                Assert.True(remaining.CanRead);

            // Simulate the storage provider becoming available and the retry
            // deadline being reached, without delaying CI for a full minute.
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE dbo.FileDeletionOutbox
                SET StorageProvider = {"Local"},
                    NotBeforeUtc = DATEADD(second, -1, SYSUTCDATETIME()),
                    LeaseUntilUtc = NULL
                WHERE Id = {jobId}
                """);

            Assert.Equal(1, await worker.ProcessBatchAsync());
            var succeeded = await db.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*) AS Value FROM dbo.FileDeletionOutbox WHERE Id = @id AND CompletedAtUtc IS NOT NULL AND AttemptCount >= 2 AND LastError IS NULL",
                new Microsoft.Data.SqlClient.SqlParameter("@id", jobId)).SingleAsync();
            Assert.Equal(1, succeeded);
            await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                await using var _ = await storage.OpenReadAsync(blob.StorageKey);
            });
            Assert.Equal(0, await worker.ProcessBatchAsync());
        }
        finally
        {
            if (storedKey is not null)
                await storage.DeleteAsync(storedKey);
        }
    }

    [Fact]
    public async Task Reconciler_removes_only_old_unregistered_bytes_and_preserves_SQL_files()
    {
        var root = Path.Combine(Path.GetTempPath(), "auran-reconcile-" + Guid.NewGuid().ToString("N"));
        var settings = Options.Create(new FileStorageOptions
        {
            RootPath = root,
            OrphanCleanupEnabled = false,
            OrphanGracePeriodHours = 24,
            OrphanCleanupIntervalMinutes = 60
        });

        try
        {
            var storage = new LocalFileStorageService(settings);
            await using var protectedStream = new MemoryStream("protected"u8.ToArray());
            var protectedFile = await storage.SaveAsync(protectedStream, "protected.pdf", "application/pdf");
            await using var orphanStream = new MemoryStream("orphan"u8.ToArray());
            var orphanFile = await storage.SaveAsync(orphanStream, "orphan.pdf", "application/pdf");
            await using var freshStream = new MemoryStream("new"u8.ToArray());
            var freshFile = await storage.SaveAsync(freshStream, "new.pdf", "application/pdf");

            var old = DateTime.UtcNow.AddDays(-3);
            File.SetLastWriteTimeUtc(Path.Combine(root, protectedFile.StorageKey.Replace('/', Path.DirectorySeparatorChar)), old);
            File.SetLastWriteTimeUtc(Path.Combine(root, orphanFile.StorageKey.Replace('/', Path.DirectorySeparatorChar)), old);

            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
                var clinicId = Guid.NewGuid();
                var userId = Guid.NewGuid();
                var identityId = Guid.NewGuid().ToString("N");
                db.Clinics.Add(new Auran.Clinic.Domain.Entities.Clinic
                {
                    Id = clinicId, Name = "Reconciliation SQL scope",
                    Code = "RC" + clinicId.ToString("N")[..12]
                });
                db.Set<ApplicationIdentityUser>().Add(new ApplicationIdentityUser
                {
                    Id = identityId,
                    UserName = "reconcile-" + identityId,
                    NormalizedUserName = "RECONCILE-" + identityId.ToUpperInvariant(),
                    Email = identityId + "@example.invalid",
                    NormalizedEmail = identityId.ToUpperInvariant() + "@EXAMPLE.INVALID",
                    SecurityStamp = Guid.NewGuid().ToString()
                });
                db.Users.Add(new User
                {
                    Id = userId, ClinicId = clinicId, IdentityUserId = identityId,
                    FullName = "Reconciliation fixture user", IsActive = true
                });
                db.Files.Add(new FileRecord
                {
                    Id = Guid.NewGuid(), ClinicId = clinicId,
                    OriginalName = "protected.pdf", StoredName = protectedFile.StoredName,
                    StorageKey = protectedFile.StorageKey, StorageProvider = "Local",
                    ContentType = "application/pdf", Size = protectedFile.Size,
                    UploadedByUserId = userId, UploadedAtUtc = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }

            var worker = new LocalOrphanFileReconciliationService(
                storage,
                factory.Services.GetRequiredService<IServiceScopeFactory>(),
                settings,
                NullLogger<LocalOrphanFileReconciliationService>.Instance);

            Assert.Equal(1, await worker.ReconcileOnceAsync());

            await using var retained = await storage.OpenReadAsync(protectedFile.StorageKey);
            Assert.NotNull(retained);
            await using var newFile = await storage.OpenReadAsync(freshFile.StorageKey);
            Assert.NotNull(newFile);
            await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                await using var _ = await storage.OpenReadAsync(orphanFile.StorageKey);
            });
            Assert.Equal(0, await worker.ReconcileOnceAsync());
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
