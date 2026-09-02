namespace Auran.Clinic.Infrastructure.Auditing;

public static class AuditRedactor
{
    private static readonly string[] SensitiveFragments =
    [
        "password", "token", "authorization", "secret", "credential", "cookie"
    ];

    public static IReadOnlyDictionary<string, object?> Redact(IReadOnlyDictionary<string, object?> metadata)
    {
        return metadata.ToDictionary(
            pair => pair.Key,
            pair => IsSensitive(pair.Key) ? "[REDACTED]" : pair.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsSensitive(string key) =>
        SensitiveFragments.Any(fragment => key.Contains(fragment, StringComparison.OrdinalIgnoreCase));
}
