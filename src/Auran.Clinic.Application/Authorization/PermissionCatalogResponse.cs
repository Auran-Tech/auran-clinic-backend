namespace Auran.Clinic.Application.Authorization;

public sealed class PermissionCatalogResponse
{
    public required string Key { get; init; }

    public required string GroupKey { get; init; }

    public IReadOnlyDictionary<string, string> Descriptions { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
