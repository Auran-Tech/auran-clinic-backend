namespace Auran.Clinic.Application.Authorization;

public sealed record SystemRoleDefinition(
    string Code,
    string Name,
    IReadOnlyCollection<string> Permissions);
