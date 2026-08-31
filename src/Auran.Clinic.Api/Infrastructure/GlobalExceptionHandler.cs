using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Diagnostics;

namespace Auran.Clinic.Api.Infrastructure;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
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
                Message = "An unexpected error occurred.",
                Error = "internal_server_error"
            },
            cancellationToken);

        return true;
    }
}
