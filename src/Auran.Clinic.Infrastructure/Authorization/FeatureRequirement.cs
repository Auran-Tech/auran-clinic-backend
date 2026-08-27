using Microsoft.AspNetCore.Authorization;

namespace Auran.Clinic.Infrastructure.Authorization;

public sealed record FeatureRequirement(string FeatureCode) : IAuthorizationRequirement;
