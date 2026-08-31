using System.Security.Claims;
using Auran.Clinic.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Auran.Clinic.Infrastructure.Authentication;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId => GetGuid("user_id");

    public Guid? ClinicId => GetGuid("clinic_id");

    public bool IsSuperUser =>
        bool.TryParse(Principal?.FindFirstValue("super_user"), out var value) && value;

    private Guid? GetGuid(string claimType) =>
        Guid.TryParse(Principal?.FindFirstValue(claimType), out var value) ? value : null;
}
