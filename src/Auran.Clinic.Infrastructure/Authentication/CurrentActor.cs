using System.Security.Claims;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Auran.Clinic.Infrastructure.Authentication;

public sealed class CurrentActor(IHttpContextAccessor httpContextAccessor) : ICurrentActor
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public ActorType ActorType
    {
        get
        {
            var value = Principal?.FindFirstValue("actor_type");
            return Enum.TryParse<ActorType>(value, true, out var actorType)
                ? actorType
                : ActorType.System;
        }
    }

    public string? IdentityUserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Principal?.FindFirstValue("sub");

    public Guid? PlatformUserId => GetGuid("platform_user_id");
    public Guid? ClinicUserId => GetGuid("clinic_user_id");
    public Guid? ClinicId => GetGuid("clinic_id");

    public bool IsClinicSuperUser =>
        bool.TryParse(Principal?.FindFirstValue("clinic_super_user"), out var value) && value;

    public string? DisplayName => Principal?.FindFirstValue("display_name");
    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email)
        ?? Principal?.FindFirstValue("email");

    private Guid? GetGuid(string claimType) =>
        Guid.TryParse(Principal?.FindFirstValue(claimType), out var value) ? value : null;
}
