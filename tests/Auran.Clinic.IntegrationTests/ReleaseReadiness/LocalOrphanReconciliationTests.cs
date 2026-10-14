using Auran.Clinic.Domain.Entities;
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
