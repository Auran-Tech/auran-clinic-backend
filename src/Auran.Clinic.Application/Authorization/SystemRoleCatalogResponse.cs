namespace Auran.Clinic.Application.Authorization;

public sealed record SystemRoleCatalogResponse(
    string Code,
    string Name,
    IReadOnlyCollection<string> Permissions);
