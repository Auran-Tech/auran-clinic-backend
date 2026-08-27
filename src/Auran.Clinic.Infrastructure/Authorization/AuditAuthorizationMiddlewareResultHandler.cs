using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Auran.Clinic.Infrastructure.Authorization;

public sealed class AuditAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            var actor = context.RequestServices.GetRequiredService<ICurrentActor>();
            var auditService = context.RequestServices.GetRequiredService<IAuditService>();
            if (actor.IsAuthenticated)
            {
                await auditService.WriteAsync(new AuditEvent
                {
                    Scope = actor.ActorType == ActorType.Clinic ? AuditScope.Clinic : AuditScope.Platform,
                    ClinicId = actor.ActorType == ActorType.Clinic ? actor.ClinicId : null,
                    Action = "Authorization.AccessDenied",
                    Category = "Security",
                    EntityType = "Endpoint",
                    EntityId = context.Request.Path.Value,
                    Description = "An authenticated actor was denied access by authorization policy.",
                    Metadata = new { Method = context.Request.Method, Path = context.Request.Path.Value }
                }, context.RequestAborted);
            }
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
