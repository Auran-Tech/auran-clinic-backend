namespace Auran.Clinic.Application.Authorization;

public sealed record PermissionDefinition(
    string Key,
    string Group,
    string EnglishDescription,
    string ArabicDescription,
    string? LegacyKey = null);
