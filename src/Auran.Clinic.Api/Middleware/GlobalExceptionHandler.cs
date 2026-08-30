using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Diagnostics;

namespace Auran.Clinic.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled request exception. TraceId: {TraceId}", httpContext.TraceIdentifier);
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new BaseResponse
        {
            Status = false,
            Message = "An unexpected error occurred.",
            Error = "INTERNAL_SERVER_ERROR"
        }, cancellationToken);
        return true;
    }
}
