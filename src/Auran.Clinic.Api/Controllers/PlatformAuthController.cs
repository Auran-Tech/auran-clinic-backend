using System.Security.Claims;
using Auran.Clinic.Api.Infrastructure;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Route("api/platform/auth")]
[Produces("application/json")]
public sealed class PlatformAuthController(IPlatformAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting(ApiSecurityPolicies.LoginRateLimit)]
    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "Authenticate an AURAN platform user",
        Description = "Authenticates an active platform account and returns a platform-scoped JWT plus rotating refresh token. Platform tokens carry no clinic context.",
        OperationId = "PlatformAuth_Login",
        Tags = new[] { "Platform Authentication" })]
    [ProducesResponseType(typeof(BaseResponse<PlatformAuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<BaseResponse<PlatformAuthResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (result is null)
            return Unauthorized(new BaseResponse { Status = false, Message = "Invalid platform credentials." });

        return Ok(new BaseResponse<PlatformAuthResponse> { Status = true, Data = result });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [SwaggerOperation(
        Summary = "Rotate a platform refresh token",
        Description = "Exchanges an active platform refresh token for a replacement session and revokes the submitted session.",
        OperationId = "PlatformAuth_RefreshToken",
        Tags = new[] { "Platform Authentication" })]
    [ProducesResponseType(typeof(BaseResponse<PlatformAuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseResponse<PlatformAuthResponse>>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RefreshAsync(request, cancellationToken);
        if (result is null)
            return Unauthorized(new BaseResponse { Status = false, Message = "Invalid or expired platform refresh token." });

        return Ok(new BaseResponse<PlatformAuthResponse> { Status = true, Data = result });
    }

    [Authorize(Policy = ActorPolicies.Platform)]
    [HttpPost("logout")]
    [SwaggerOperation(
        Summary = "Log out a platform user",
        Description = "Revokes the supplied refresh session when it belongs to the authenticated platform actor.",
        OperationId = "PlatformAuth_Logout",
        Tags = new[] { "Platform Authentication" })]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseResponse>> Logout(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue("platform_user_id"), out var platformUserId))
            return Unauthorized(new BaseResponse { Status = false, Message = "Invalid platform session." });

        await authService.RevokeAsync(platformUserId, request.RefreshToken, cancellationToken);
        return Ok(new BaseResponse { Status = true, Message = "Logged out successfully." });
    }
}
