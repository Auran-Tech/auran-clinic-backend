using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Auran.Clinic.Infrastructure.Authorization;

public sealed class AuditAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            await AuditForbiddenAsync(context);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new BaseResponse
            {
                Status = false,
                Message = "Forbidden.",
                Error = "You do not have permission to perform this action."
            }, context.RequestAborted);
            return;
        }

        if (authorizeResult.Challenged)
        {
            await context.ChallengeAsync();
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new BaseResponse
                {
                    Status = false,
                    Message = "Unauthorized.",
                    Error = "Authentication is required or the access token is invalid, expired or revoked."
                }, context.RequestAborted);
            }
            return;
        }

        await next(context);
    }

    private static async Task AuditForbiddenAsync(HttpContext context)
    {
        var actor = context.RequestServices.GetRequiredService<ICurrentActor>();
        if (!actor.IsAuthenticated)
            return;

        var auditService = context.RequestServices.GetRequiredService<IAuditService>();
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
