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
