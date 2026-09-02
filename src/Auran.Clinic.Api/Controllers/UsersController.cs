using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Users;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
[Produces("application/json")]
public sealed class UsersController(
    IUserAccountService userAccountService,
    IValidator<UpdateUserStatusRequest> updateUserStatusValidator) : ControllerBase
{
    [HttpPut("status")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Users.ManageStatus)]
    [SwaggerOperation(
        Summary = "Change a clinic user's business account status",
        Description = "Activates or deactivates a user in the authenticated clinic. Requires Users_Manage_Status or clinic Super User access. A non-Super User cannot change another protected Super User. The last active clinic Super User cannot be deactivated. Deactivation revokes all active refresh sessions for the target user.",
        OperationId = "Users_SetStatus",
        Tags = new[] { "Users" })]
    [ProducesResponseType(typeof(BaseResponse<UserAccountStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BaseResponse<UserAccountStatusResponse>>> SetStatus(
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await updateUserStatusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new BaseResponse
            {
                Status = false,
                Message = string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)),
                Error = "validation_error"
            });
        }

        return MapResult(await userAccountService.SetStatusAsync(request, cancellationToken));
    }

    [HttpPost("disable-self")]
    [SwaggerOperation(
        Summary = "Disable the authenticated user's business account",
        Description = "Allows the authenticated clinic user to disable their own business account without Users_Manage_Status. The last active clinic Super User cannot disable their account. A successful operation revokes all refresh sessions for the account; subsequent access-token and refresh-token requests are rejected.",
        OperationId = "Users_DisableSelf",
        Tags = new[] { "Users" })]
    [ProducesResponseType(typeof(BaseResponse<UserAccountStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BaseResponse<UserAccountStatusResponse>>> DisableSelf(
        CancellationToken cancellationToken)
    {
        return MapResult(await userAccountService.DisableCurrentAsync(cancellationToken));
    }

    private ActionResult<BaseResponse<UserAccountStatusResponse>> MapResult(UserAccountStatusResult result)
    {
        return result.Outcome switch
        {
            UserAccountStatusOutcome.Success => Ok(new BaseResponse<UserAccountStatusResponse>
            {
                Status = true,
                Data = result.User
            }),
            UserAccountStatusOutcome.NotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "User not found."
            }),
            UserAccountStatusOutcome.Forbidden => StatusCode(
                StatusCodes.Status403Forbidden,
                new BaseResponse
                {
                    Status = false,
                    Message = "You are not allowed to change this user's account status."
                }),
            UserAccountStatusOutcome.Conflict => Conflict(new BaseResponse
            {
                Status = false,
                Message = "The clinic must keep at least one active Super User.",
                Error = "last_superuser_required"
            }),
            UserAccountStatusOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(
                StatusCodes.Status500InternalServerError,
                new BaseResponse
                {
                    Status = false,
                    Message = "Unable to change the user account status."
                })
        };
    }
}
