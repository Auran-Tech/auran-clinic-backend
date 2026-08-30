using System.Security.Cryptography;
using System.Text;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

internal static class PermissionSeedIds
{
    public static Guid Permission(string key) => Create("permission:" + key);

    public static Guid Translation(string key, string languageCode) =>
        Create($"permission-translation:{key}:{languageCode}");

    private static Guid Create(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash.AsSpan(0, 16));
    }
}
