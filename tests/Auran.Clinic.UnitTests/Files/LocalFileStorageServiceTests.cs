using Auran.Clinic.Infrastructure.Files;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.UnitTests.Files;

public sealed class LocalFileStorageServiceTests
{
    [Fact]
    public async Task Failed_stream_read_removes_partial_stored_file()
    {
        var root = Path.Combine(Path.GetTempPath(), "auran-files-" + Guid.NewGuid().ToString("N"));

        try
        {
            var service = new LocalFileStorageService(
                Options.Create(new FileStorageOptions { RootPath = root }));
            await using var failingInput = new FailingReadStream(new byte[160_000]);

            await Assert.ThrowsAsync<IOException>(() =>
                service.SaveAsync(failingInput, "patient.pdf", "application/pdf"));

            Assert.Empty(Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private sealed class FailingReadStream(byte[] bytes) : MemoryStream(bytes)
    {
        // MemoryStream optimizes CopyToAsync and can bypass ReadAsync overrides.
        // Emulate a failed copy after some protected bytes have reached the disk.
        public override async Task CopyToAsync(
            Stream destination,
            int bufferSize,
            CancellationToken cancellationToken)
        {
            await destination.WriteAsync(new byte[4096], cancellationToken);
            throw new IOException("Simulated upload stream failure.");
        }
    }

    [Theory]
    [InlineData("../outside.pdf")]
    [InlineData("2026/../outside.pdf")]
    [InlineData("2026/./file.pdf")]
    [InlineData("2026\\\\..\\\\outside.pdf")]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\\\Windows\\\\system.ini")]
    public async Task Storage_operations_reject_untrusted_keys(string key)
    {
        var root = Path.Combine(Path.GetTempPath(), "auran-files-" + Guid.NewGuid().ToString("N"));
        try
        {
            var service = new LocalFileStorageService(
                Options.Create(new FileStorageOptions { RootPath = root }));
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.OpenReadAsync(key));
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(key));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Storage_rejects_child_directory_symlink_that_escapes_root()
    {
        var root = Path.Combine(Path.GetTempPath(), "auran-files-" + Guid.NewGuid().ToString("N"));
        var outside = Path.Combine(Path.GetTempPath(), "auran-outside-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outside);
        await File.WriteAllTextAsync(Path.Combine(outside, "secret.pdf"), "sensitive");
        try
        {
            var service = new LocalFileStorageService(
                Options.Create(new FileStorageOptions { RootPath = root }));
            Directory.CreateSymbolicLink(Path.Combine(root, "linked"), outside);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.OpenReadAsync("linked/secret.pdf"));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.DeleteAsync("linked/secret.pdf"));
            Assert.True(File.Exists(Path.Combine(outside, "secret.pdf")));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
            if (Directory.Exists(outside))
                Directory.Delete(outside, recursive: true);
        }
    }

    [Fact]
    public async Task Orphan_candidate_scan_returns_only_aged_generated_files()
    {
        var root = Path.Combine(Path.GetTempPath(), "auran-files-" + Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new LocalFileStorageService(
                Options.Create(new FileStorageOptions { RootPath = root }));

            await using var first = new MemoryStream("old"u8.ToArray());
            var aged = await storage.SaveAsync(first, "old.pdf", "application/pdf");
            var oldFile = Path.Combine(root, aged.StorageKey.Replace('/', Path.DirectorySeparatorChar));
            File.SetLastWriteTimeUtc(oldFile, DateTime.UtcNow.AddDays(-3));

            await using var second = new MemoryStream("fresh"u8.ToArray());
            var recent = await storage.SaveAsync(second, "recent.pdf", "application/pdf");

            var arbitrary = Path.Combine(root, "2026", "10", "patient-notes.pdf");
            Directory.CreateDirectory(Path.GetDirectoryName(arbitrary)!);
            await File.WriteAllTextAsync(arbitrary, "legacy");
            File.SetLastWriteTimeUtc(arbitrary, DateTime.UtcNow.AddDays(-3));

            var candidates = storage.GetOrphanCandidates(DateTime.UtcNow.AddHours(-24));
            Assert.Contains(aged.StorageKey, candidates);
            Assert.DoesNotContain(recent.StorageKey, candidates);
            Assert.DoesNotContain("2026/10/patient-notes.pdf", candidates);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Orphan_cleanup_is_disabled_by_default()
    {
        Assert.False(new FileStorageOptions().OrphanCleanupEnabled);
        Assert.True(new FileStorageOptions().OrphanGracePeriodHours >= 24);
    }

    [Fact]
    public async Task Save_open_and_delete_round_trip()
    {
        var root = Path.Combine(Path.GetTempPath(), "auran-files-" + Guid.NewGuid().ToString("N"));

        try
        {
            var service = new LocalFileStorageService(
                Options.Create(new FileStorageOptions { RootPath = root }));

            await using var input = new MemoryStream("hello"u8.ToArray());
            var stored = await service.SaveAsync(input, "sample.pdf", "application/pdf");

            Assert.Equal("Local", stored.Provider);
            Assert.EndsWith(".pdf", stored.StoredName);

            await using var output = await service.OpenReadAsync(stored.StorageKey);
            using var reader = new StreamReader(output);
            Assert.Equal("hello", await reader.ReadToEndAsync());

            await service.DeleteAsync(stored.StorageKey);

            await Assert.ThrowsAsync<FileNotFoundException>(
                async () =>
                {
                    await using var _ = await service.OpenReadAsync(stored.StorageKey);
                });
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
