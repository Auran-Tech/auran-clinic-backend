using System.Text.Json;
using System.Text.Json.Nodes;

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

    public static object? Sanitize(string propertyName, object? value) =>
        IsSensitive(propertyName) ? "[REDACTED]" : value;

    public static string? Serialize(object? value)
    {
        if (value is null)
            return null;

        var node = JsonSerializer.SerializeToNode(value);
        Redact(node);
        return node?.ToJsonString();
    }

    private static bool IsSensitive(string propertyName) =>
        SensitiveNameFragments.Any(fragment => propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static void Redact(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(x => x.Key).ToList())
            {
                if (IsSensitive(key))
                {
                    obj[key] = "[REDACTED]";
                    continue;
                }

                Redact(obj[key]);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
                Redact(item);
        }
    }
}
