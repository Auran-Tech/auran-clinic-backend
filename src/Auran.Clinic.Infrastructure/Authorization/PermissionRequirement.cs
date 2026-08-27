using Auran.Clinic.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Auran.Clinic.Infrastructure.Authorization;

public sealed record PermissionRequirement(
    string Permission,
    PermissionScope Scope) : IAuthorizationRequirement;
