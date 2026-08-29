using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Route("api/platform/auth")]
[Produces("application/json")]
public sealed class PlatformAuthController(IPlatformAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "Authenticate an AURAN platform user",
        Description = "Authenticates an AURAN platform account. Platform tokens do not contain a clinic context and cannot be used as clinic-user tokens. Returns platform roles and platform permissions used for tenant lifecycle administration.",
        OperationId = "PlatformAuth_Login",
        Tags = new[] { "Platform Authentication" })]
    [ProducesResponseType(typeof(BaseResponse<PlatformAuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
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
        Description = "Rotates a valid AURAN platform refresh token and returns a replacement platform session. Rotation revokes the previous session, so its access token can no longer authorize protected platform endpoints.",
        OperationId = "PlatformAuth_RefreshToken",
        Tags = new[] { "Platform Authentication" })]
    [ProducesResponseType(typeof(BaseResponse<PlatformAuthResponse>), StatusCodes.Status200OK)]
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
        Description = "Revokes the supplied platform authentication session. The access token associated with that session becomes invalid immediately for protected platform endpoints. Requires a valid platform JWT.",
        OperationId = "PlatformAuth_Logout",
        Tags = new[] { "Platform Authentication" })]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseResponse>> Logout(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        await authService.RevokeAsync(request.RefreshToken, cancellationToken);
        return Ok(new BaseResponse { Status = true, Message = "Logged out successfully." });
    }
}
