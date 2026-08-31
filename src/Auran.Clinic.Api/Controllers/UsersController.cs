using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
public sealed class UsersController(IUserAccountService userAccountService) : ControllerBase
{
    [Authorize(Policy = PermissionPolicy.ClinicPrefix + Permissions.Clinic.Users.ManageStatus)]
    [HttpPut("status")]
    [SwaggerOperation(
        Summary = "Enable or disable a clinic user account",
        Description = "Changes account status inside the authenticated clinic. A normal manager cannot disable another protected Super User. Disabling an account revokes its refresh tokens and invalidates existing JWT requests immediately.",
        OperationId = "Users_UpdateStatus",
        Tags = new[] { "Users" })]
    public async Task<ActionResult<BaseResponse>> UpdateStatus(
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userAccountService.SetStatusAsync(request.UserId, request.IsActive, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize]
    [HttpPost("disable-self")]
    [SwaggerOperation(
        Summary = "Disable the current account",
        Description = "Allows the authenticated clinic user to disable their own account. Refresh tokens are revoked and subsequent JWT requests are rejected until an authorized user re-enables the account.",
        OperationId = "Users_DisableSelf",
        Tags = new[] { "Users" })]
    public async Task<ActionResult<BaseResponse>> DisableSelf(CancellationToken cancellationToken)
    {
        var result = await userAccountService.DisableCurrentAsync(cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult<BaseResponse> ToActionResult(UserAccountStatusResult result)
    {
        if (result.Success)
            return Ok(new BaseResponse { Status = true, Message = "Account status updated successfully." });

        var response = new BaseResponse
        {
            Status = false,
            Error = result.ErrorCode,
            Message = result.ErrorCode switch
            {
                "NOT_FOUND" => "User was not found.",
                "SUPER_USER_PROTECTED" => "The protected Super User account cannot be disabled by this user.",
                _ => "The account status could not be updated."
            }
        };

        return result.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(response),
            "SUPER_USER_PROTECTED" => StatusCode(StatusCodes.Status403Forbidden, response),
            _ => Unauthorized(response)
        };
    }
}
