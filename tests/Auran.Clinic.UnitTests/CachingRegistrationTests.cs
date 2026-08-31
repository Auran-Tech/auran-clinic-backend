using Auran.Clinic.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace Auran.Clinic.UnitTests;

public class CachingRegistrationTests
{
    [Fact]
    public void AddAuranCaching_RegistersDistributedMemoryCache()
    {
        var services = new ServiceCollection();
        services.AddAuranCaching();

        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<IDistributedCache>();

        Assert.Equal("MemoryDistributedCache", cache.GetType().Name);
    }

    [Fact]
    public void Repository_DoesNotContainLegacyExternalCacheReferences()
    {
        var repositoryRoot = FindRepositoryRoot();
        var forbiddenToken = string.Concat("Re", "dis");
        var violations = Directory
            .EnumerateFiles(repositoryRoot, "*", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedOrToolingPath(path, repositoryRoot))
            .Where(path => !IsKnownBinaryFile(path))
            .Where(path => File.ReadAllText(path).Contains(forbiddenToken, StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Legacy external-cache references remain in: {string.Join(", ", violations)}");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Auran.Clinic.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from the test working directory.");
    }

    private static bool IsKnownBinaryFile(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() is
            ".dll" or ".pdb" or ".exe" or ".so" or ".dylib" or
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".ico" or
            ".zip" or ".gz" or ".7z" or ".pdf" or
            ".woff" or ".woff2" or ".ttf" or ".eot";
    }

    private static bool IsGeneratedOrToolingPath(string path, string repositoryRoot)
    {
        var relativePath = Path.GetRelativePath(repositoryRoot, path);
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return segments.Any(segment => segment is ".git" or "bin" or "obj" or "artifacts");
    }
}
