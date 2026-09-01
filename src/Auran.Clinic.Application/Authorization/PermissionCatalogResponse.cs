namespace Auran.Clinic.Application.Authorization;

public sealed class PermissionCatalogResponse
{
    public required string Key { get; init; }
    public required string Group { get; init; }
    public required Dictionary<string, string> Descriptions { get; init; }
}
