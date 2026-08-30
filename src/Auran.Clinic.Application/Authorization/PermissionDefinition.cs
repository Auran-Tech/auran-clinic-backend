using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Application.Authorization;

public sealed record PermissionDefinition(
    string Key,
    string Group,
    PermissionScope Scope,
    string EnglishDescription,
    string ArabicDescription);
