using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "Authenticate a clinic user",
        Description = "Validates credentials and clinic/account status. Returns JWT access and refresh tokens plus the effective backend-calculated permissions for the authenticated user.",
        OperationId = "Auth_Login",
        Tags = new[] { "Authentication" })]
    [ProducesResponseType(typeof(BaseResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<BaseResponse<AuthResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (result is null)
            return Unauthorized(new BaseResponse { Status = false, Message = "Invalid credentials or inactive account." });

        return Ok(new BaseResponse<AuthResponse> { Status = true, Data = result });
    }

    [Authorize]
    [HttpGet("me")]
    [SwaggerOperation(
        Summary = "Get the current authenticated clinic user",
        Description = "Returns the current user, clinic context, assigned roles and effective backend-calculated permissions. Super Users receive the complete permission catalog.",
        OperationId = "Auth_GetCurrentUser",
        Tags = new[] { "Authentication" })]
    [ProducesResponseType(typeof(BaseResponse<CurrentUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BaseResponse<CurrentUserResponse>>> Me(CancellationToken cancellationToken)
    {
        var result = await authService.GetCurrentAsync(cancellationToken);
        if (result is null)
            return Unauthorized(new BaseResponse { Status = false, Message = "The account or clinic is not active." });

        return Ok(new BaseResponse<CurrentUserResponse> { Status = true, Data = result });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [SwaggerOperation(
        Summary = "Rotate an authentication refresh token",
        Description = "Exchanges an active refresh token for a replacement token pair. Rotation is concurrency-protected and also validates that the user and clinic are active.",
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
        Description = "Revokes the supplied refresh token only when it belongs to the authenticated user and clinic.",
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
