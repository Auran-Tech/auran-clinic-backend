namespace Auran.Clinic.Application.Authorization;

public sealed record RoleCatalogResponse(
    string Code,
    string Name,
    IReadOnlyCollection<string> Permissions);
