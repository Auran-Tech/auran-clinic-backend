using Auran.Clinic.Application.Authentication;
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
        Description = "Validates the supplied email and password. On success, returns a JWT access token, a refresh token, the authenticated domain user, clinic context, assigned roles, and effective permissions. Use the access token as a Bearer token for protected endpoints and persist the refresh token securely for token rotation.",
        OperationId = "Auth_Login",
        Tags = new[] { "Authentication" })]
    [ProducesResponseType(typeof(BaseResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
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
        Summary = "Rotate an authentication refresh token",
        Description = "Exchanges a valid, active refresh token for a new JWT access token and a replacement refresh token. The submitted refresh token is revoked during successful rotation and must not be reused. Returns 401 when the token is invalid, expired, revoked, or no longer associated with an active user.",
        OperationId = "Auth_RefreshToken",
        Tags = new[] { "Authentication" })]
    [ProducesResponseType(typeof(BaseResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
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

    [Authorize]
    [HttpPost("logout")]
    [SwaggerOperation(
        Summary = "Log out and revoke a refresh token",
        Description = "Revokes the supplied refresh token for the authenticated user. This endpoint requires a valid JWT Bearer access token. After a successful response, the revoked refresh token cannot be used to obtain another access token.",
        OperationId = "Auth_Logout",
        Tags = new[] { "Authentication" })]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseResponse>> Logout(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        await authService.RevokeAsync(request.RefreshToken, cancellationToken);
        return Ok(new BaseResponse { Status = true, Message = "Logged out successfully." });
    }
}
