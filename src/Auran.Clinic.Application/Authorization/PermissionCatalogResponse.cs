using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Application.Authorization;

public sealed class PermissionCatalogResponse
{
    public required string Key { get; init; }
    public required string Group { get; init; }
    public PermissionScope Scope { get; init; }
    public Dictionary<string, string> Descriptions { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
