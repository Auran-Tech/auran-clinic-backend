using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Application.Authorization;

public sealed record PermissionDefinition(
    string Code,
    string Name,
    string Group,
    PermissionScope Scope);
