using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<BaseResponse<AuthResponse>>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (result is null)
            return Unauthorized(new BaseResponse { Status = false, Message = "Invalid email or password." });

        return Ok(new BaseResponse<AuthResponse> { Status = true, Data = result });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<BaseResponse<AuthResponse>>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RefreshAsync(request, cancellationToken);
        if (result is null)
            return Unauthorized(new BaseResponse { Status = false, Message = "Invalid or expired refresh token." });

        return Ok(new BaseResponse<AuthResponse> { Status = true, Data = result });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<BaseResponse>> Logout(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await authService.RevokeAsync(request.RefreshToken, cancellationToken);
        return Ok(new BaseResponse { Status = true, Message = "Logged out successfully." });
    }
}
