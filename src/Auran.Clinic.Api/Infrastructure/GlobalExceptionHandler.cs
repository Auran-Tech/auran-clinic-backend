using Auran.Clinic.Application.Localization;
using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Localization;

namespace Auran.Clinic.Api.Infrastructure;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IStringLocalizer<ApiMessages> localizer) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Unhandled exception. TraceIdentifier: {TraceIdentifier}",
            httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(
            new BaseResponse
            {
                Status = false,
                Message = localizer[ApiMessageKeys.InternalServerError].Value,
                Error = "internal_server_error"
            },
            cancellationToken);

        return true;
    }
}
