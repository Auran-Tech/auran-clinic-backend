using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "Authenticate a clinic user",
        Description = "Authenticates a clinic account only. On success returns a clinic-scoped JWT, rotating refresh token, clinic context, roles and effective clinic permissions. Platform accounts must use /api/platform/auth/login.",
        OperationId = "Auth_Login",
        Tags = new[] { "Authentication" })]
    [ProducesResponseType(typeof(BaseResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseResponse<AuthResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (result is null)
            return Unauthorized(new BaseResponse { Status = false, Message = "Invalid email or password." });
        return Ok(new BaseResponse<AuthResponse> { Status = true, Data = result });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [SwaggerOperation(
        Summary = "Rotate a clinic refresh token",
        Description = "Exchanges a valid clinic refresh token for a new clinic JWT and replacement refresh token. Suspended clinics cannot refresh sessions.",
        OperationId = "Auth_RefreshToken",
        Tags = new[] { "Authentication" })]
    [ProducesResponseType(typeof(BaseResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseResponse<AuthResponse>>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RefreshAsync(request, cancellationToken);
        if (result is null)
            return Unauthorized(new BaseResponse { Status = false, Message = "Invalid or expired refresh token." });
        return Ok(new BaseResponse<AuthResponse> { Status = true, Data = result });
    }

    [Authorize(Policy = ActorPolicies.Clinic)]
    [HttpPost("logout")]
    [SwaggerOperation(
        Summary = "Log out a clinic user",
        Description = "Revokes the supplied clinic refresh token. Requires a valid active-clinic JWT.",
        OperationId = "Auth_Logout",
        Tags = new[] { "Authentication" })]
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
