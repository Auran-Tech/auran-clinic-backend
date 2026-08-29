namespace Auran.Clinic.Infrastructure.Authentication;

public static class BearerTokenNormalizer
{
    private const string BearerPrefix = "Bearer ";

    public static string? NormalizeAuthorizationHeader(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
            return null;

        var value = authorizationHeader.Trim();
        if (!value.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        value = value[BearerPrefix.Length..].Trim();

        while (value.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            value = value[BearerPrefix.Length..].Trim();

        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') ||
             (value[0] == '\'' && value[^1] == '\'')))
        {
            value = value[1..^1].Trim();
        }

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
