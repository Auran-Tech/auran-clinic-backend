using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Users;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/users")]
[Produces("application/json")]
public sealed class UsersController(
    IUserAccountService userAccountService,
    IValidator<UpdateUserStatusRequest> updateUserStatusValidator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Users.View)]
    [SwaggerOperation(
        Summary = "List clinic users",
        Description = "Returns users in the authenticated clinic with their protected Super User flag, active state, and assigned system roles.",
        OperationId = "Users_List",
        Tags = new[] { "Users" })]
    public async Task<ActionResult<BaseResponse<IReadOnlyList<UserAccountResponse>>>> List(
        CancellationToken cancellationToken)
    {
        var users = await userAccountService.ListAsync(cancellationToken);
        return Ok(new BaseResponse<IReadOnlyList<UserAccountResponse>>
        {
            Status = true,
            Data = users
        });
    }

    [HttpPost]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Users.Manage)]
    [SwaggerOperation(
        Summary = "Create a clinic user",
        Description = "Creates an ASP.NET Identity credential and matching clinic business user atomically. Normal users require at least one protected system role. Only a Clinic Super User can create another Super User.",
        OperationId = "Users_Create",
        Tags = new[] { "Users" })]
    public async Task<ActionResult<BaseResponse<UserAccountResponse>>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        return MapManagementResult(await userAccountService.CreateAsync(request, cancellationToken), created: true);
    }

    [HttpPut("{userId:guid}")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Users.Manage)]
    [SwaggerOperation(
        Summary = "Update a clinic user",
        Description = "Updates the user's business profile and Identity email/username in the authenticated clinic. Protected Super Users cannot be modified by normal managers.",
        OperationId = "Users_Update",
        Tags = new[] { "Users" })]
    public async Task<ActionResult<BaseResponse<UserAccountResponse>>> Update(
        Guid userId,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        return MapManagementResult(await userAccountService.UpdateAsync(userId, request, cancellationToken));
    }

    [HttpPut("{userId:guid}/roles")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Roles.Manage)]
    [SwaggerOperation(
        Summary = "Replace a clinic user's role assignments",
        Description = "Replaces the user's protected system-role assignments and revokes all active sessions so new permissions take effect immediately.",
        OperationId = "Users_SetRoles",
        Tags = new[] { "Users", "RBAC" })]
    public async Task<ActionResult<BaseResponse<UserAccountResponse>>> SetRoles(
        Guid userId,
        [FromBody] SetUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        return MapManagementResult(await userAccountService.SetRolesAsync(userId, request, cancellationToken));
    }

    [HttpPut("status")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Users.ManageStatus)]
    [SwaggerOperation(
        Summary = "Change a clinic user's business account status",
        Description = "Activates or deactivates a user in the authenticated clinic. A non-Super User cannot change another protected Super User. The last active clinic Super User cannot be deactivated. Deactivation revokes all active sessions for the target user.",
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

        return MapStatusResult(await userAccountService.SetStatusAsync(request, cancellationToken));
    }

    [HttpPost("disable-self")]
    [SwaggerOperation(
        Summary = "Disable the authenticated user's business account",
        Description = "Allows the authenticated clinic user to disable their own business account without Users_Manage_Status. The last active clinic Super User cannot disable their account. A successful operation revokes all sessions for the account.",
        OperationId = "Users_DisableSelf",
        Tags = new[] { "Users" })]
    public async Task<ActionResult<BaseResponse<UserAccountStatusResponse>>> DisableSelf(
        CancellationToken cancellationToken)
    {
        return MapStatusResult(await userAccountService.DisableCurrentAsync(cancellationToken));
    }

    private ActionResult<BaseResponse<UserAccountResponse>> MapManagementResult(
        UserManagementResult result,
        bool created = false)
    {
        return result.Outcome switch
        {
            UserManagementOutcome.Success when created => StatusCode(
                StatusCodes.Status201Created,
                new BaseResponse<UserAccountResponse> { Status = true, Data = result.User }),
            UserManagementOutcome.Success => Ok(new BaseResponse<UserAccountResponse>
            {
                Status = true,
                Data = result.User
            }),
            UserManagementOutcome.NotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "User not found."
            }),
            UserManagementOutcome.Forbidden => StatusCode(
                StatusCodes.Status403Forbidden,
                new BaseResponse
                {
                    Status = false,
                    Message = result.Error ?? "You are not allowed to manage this user."
                }),
            UserManagementOutcome.Conflict => Conflict(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "The requested user change conflicts with existing data.",
                Error = "user_conflict"
            }),
            UserManagementOutcome.ValidationError => BadRequest(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Validation failed.",
                Error = "validation_error"
            }),
            UserManagementOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to manage the user."
            })
        };
    }

    private ActionResult<BaseResponse<UserAccountStatusResponse>> MapStatusResult(UserAccountStatusResult result)
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
