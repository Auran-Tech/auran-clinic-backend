namespace Auran.Clinic.Application.Authorization;

public sealed record PermissionDefinition(
    string Key,
    string GroupKey,
    string EnglishDescription,
    string ArabicDescription);
