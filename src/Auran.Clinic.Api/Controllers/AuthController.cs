using Auran.Clinic.Api.Contracts.Authentication;
using Auran.Clinic.Api.Mappings;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Authorization;
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
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    [SwaggerOperation(Summary = "Authenticate a clinic user", OperationId = "Auth_Login", Tags = new[] { "Authentication" })]
    public async Task<ActionResult<BaseResponse<AuthResponse>>> Login(
        [FromBody] LoginApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request.ToServiceRequest(), cancellationToken);
        if (result is null)
            return Unauthorized(new BaseResponse { Status = false, Message = "Invalid credentials or inactive account." });
        return Ok(new BaseResponse<AuthResponse> { Status = true, Data = result });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [SwaggerOperation(Summary = "Rotate a clinic refresh token", OperationId = "Auth_RefreshToken", Tags = new[] { "Authentication" })]
    public async Task<ActionResult<BaseResponse<AuthResponse>>> Refresh(
        [FromBody] RefreshTokenApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RefreshAsync(request.ToServiceRequest(), cancellationToken);
        if (result is null)
            return Unauthorized(new BaseResponse { Status = false, Message = "Invalid or expired refresh token." });
        return Ok(new BaseResponse<AuthResponse> { Status = true, Data = result });
    }

    [Authorize(Policy = ActorPolicies.Clinic)]
    [HttpGet("me")]
    [SwaggerOperation(
        Summary = "Get the current clinic user",
        Description = "Returns the backend-calculated effective roles and permission keys. Super Users receive every clinic permission key from the backend.",
        OperationId = "Auth_Me",
        Tags = new[] { "Authentication" })]
    public async Task<ActionResult<BaseResponse<CurrentUserResponse>>> Me(CancellationToken cancellationToken)
    {
        var result = await authService.GetCurrentAsync(cancellationToken);
        if (result is null)
            return Unauthorized(new BaseResponse { Status = false, Message = "Authentication session is not active." });
        return Ok(new BaseResponse<CurrentUserResponse> { Status = true, Data = result });
    }

    [Authorize(Policy = ActorPolicies.Clinic)]
    [HttpPost("logout")]
    [SwaggerOperation(Summary = "Log out a clinic user", OperationId = "Auth_Logout", Tags = new[] { "Authentication" })]
    public async Task<ActionResult<BaseResponse>> Logout(
        [FromBody] RefreshTokenApiRequest request,
        CancellationToken cancellationToken)
    {
        var serviceRequest = request.ToServiceRequest();
        await authService.RevokeAsync(serviceRequest.RefreshToken, cancellationToken);
        return Ok(new BaseResponse { Status = true, Message = "Logged out successfully." });
    }
}
