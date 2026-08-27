namespace Auran.Clinic.Infrastructure.Auditing;

internal static class AuditRedactor
{
    private static readonly string[] SensitiveNameFragments =
    {
        "password",
        "token",
        "secret",
        "signingkey",
        "connectionstring",
        "apikey",
        "credential"
    };

    public static object? Sanitize(string propertyName, object? value)
    {
        if (SensitiveNameFragments.Any(fragment => propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            return "[REDACTED]";

        return value;
    }
}
