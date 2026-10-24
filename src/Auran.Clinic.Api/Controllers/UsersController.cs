using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Users;
using Auran.Clinic.Api.Infrastructure;
using FluentValidation;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

public sealed class UploadOwnPhotoForm
{
    public required IFormFile File { get; init; }
}

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/users")]
[Produces("application/json")]
public sealed class UsersController(
    IUserAccountService userAccountService,
    IValidator<UpdateUserRequest> updateUserValidator,
    IValidator<UpdateCurrentProfileRequest> updateCurrentProfileValidator,
    IValidator<ChangeOwnPasswordRequest> changeOwnPasswordValidator,
    IValidator<RevokeOwnSessionRequest> revokeOwnSessionValidator,
    IValidator<SetUserRolesRequest> setUserRolesValidator,
    IValidator<UpdateUserStatusRequest> updateUserStatusValidator) : ControllerBase
{
    [HttpGet("me")]
    [SwaggerOperation(
        Summary = "Get the authenticated clinic user's profile",
        Description = "Returns the current clinic user's profile without requiring employee-management permission.",
        OperationId = "Users_GetCurrent",
        Tags = new[] { "Users" })]
    public async Task<ActionResult<BaseResponse<UserAccountResponse>>> GetCurrent(
        CancellationToken cancellationToken)
    {
        var user = await userAccountService.GetCurrentAsync(cancellationToken);
        return user is null
            ? NotFound(new BaseResponse { Status = false, Message = "User not found." })
            : Ok(new BaseResponse<UserAccountResponse> { Status = true, Data = user });
    }

    [HttpPut("me")]
    [SwaggerOperation(
        Summary = "Update the authenticated clinic user's profile",
        Description = "Updates only the current clinic user's name and email. The user ID is taken from the authenticated session.",
        OperationId = "Users_UpdateCurrent",
        Tags = new[] { "Users" })]
    public async Task<ActionResult<BaseResponse<UserAccountResponse>>> UpdateCurrent(
        [FromBody] UpdateCurrentProfileRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await updateCurrentProfileValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailure<UserAccountResponse>();

        return MapManagementResult(await userAccountService.UpdateCurrentAsync(request, cancellationToken));
    }

    [HttpPost("me/password")]
    [EnableRateLimiting(ApiSecurityPolicies.LoginRateLimit)]
    [SwaggerOperation(
        Summary = "Change the authenticated clinic user's password",
        Description = "Requires the current password and revokes all clinic sessions after success. The user must sign in again.",
        OperationId = "Users_ChangeOwnPassword",
        Tags = new[] { "Users", "Authentication" })]
    public async Task<ActionResult<BaseResponse>> ChangeOwnPassword(
        [FromBody] ChangeOwnPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await changeOwnPasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse { Status = false, Error = "validation_error" });

        var result = await userAccountService.ChangeOwnPasswordAsync(request, cancellationToken);
        return result.Outcome switch
        {
            UserManagementOutcome.Success => Ok(new BaseResponse { Status = true }),
            UserManagementOutcome.ValidationError => BadRequest(new BaseResponse
            {
                Status = false,
                Error = "validation_error",
                Message = result.Error ?? "Unable to change password."
            }),
            UserManagementOutcome.NotFound => NotFound(new BaseResponse { Status = false }),
            _ => Unauthorized(new BaseResponse { Status = false })
        };
    }

    [HttpGet("me/sessions")]
    [SwaggerOperation(
        Summary = "List the authenticated clinic user's active sessions",
        OperationId = "Users_ListOwnSessions",
        Tags = new[] { "Users", "Authentication" })]
    public async Task<ActionResult<BaseResponse<IReadOnlyList<OwnSessionResponse>>>> ListOwnSessions(
        CancellationToken cancellationToken)
    {
        Guid.TryParse(User.FindFirstValue("session_id"), out var currentSessionId);
        var sessions = await userAccountService.ListOwnSessionsAsync(currentSessionId, cancellationToken);
        return Ok(new BaseResponse<IReadOnlyList<OwnSessionResponse>>
        {
            Status = true,
            Data = sessions
        });
    }

    [HttpPost("me/sessions/revoke")]
    [SwaggerOperation(
        Summary = "Revoke one of the authenticated clinic user's sessions",
        OperationId = "Users_RevokeOwnSession",
        Tags = new[] { "Users", "Authentication" })]
    public async Task<ActionResult<BaseResponse>> RevokeOwnSession(
        [FromBody] RevokeOwnSessionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await revokeOwnSessionValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse { Status = false, Error = "validation_error" });

        var revoked = await userAccountService.RevokeOwnSessionAsync(
            request.SessionId, cancellationToken);
        return revoked
            ? Ok(new BaseResponse { Status = true })
            : NotFound(new BaseResponse { Status = false, Message = "Session not found." });
    }

    [HttpPost("me/sessions/revoke-all")]
    [SwaggerOperation(
        Summary = "Revoke every session owned by the authenticated clinic user",
        Description = "Includes the current session, so the user must sign in again.",
        OperationId = "Users_RevokeAllOwnSessions",
        Tags = new[] { "Users", "Authentication" })]
    public async Task<ActionResult<BaseResponse>> RevokeAllOwnSessions(
        CancellationToken cancellationToken)
    {
        await userAccountService.RevokeAllOwnSessionsAsync(cancellationToken);
        return Ok(new BaseResponse { Status = true });
    }

    [HttpGet("me/photo")]
    [SwaggerOperation(
        Summary = "Get the authenticated clinic user's profile photo",
        OperationId = "Users_GetOwnPhoto",
        Tags = new[] { "Users" })]
    public async Task<IActionResult> GetOwnPhoto(CancellationToken cancellationToken)
    {
        var photo = await userAccountService.GetOwnPhotoAsync(cancellationToken);
        return photo is null
            ? NotFound()
            : File(photo.Value.ImageData, photo.Value.ContentType);
    }

    [HttpPost("me/photo")]
    [RequestSizeLimit(ProfileImageValidator.MaxBytes + 1024)]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "Upload the authenticated clinic user's profile photo",
        Description = "Accepts JPEG, PNG, or WebP images up to 2 MB.",
        OperationId = "Users_UploadOwnPhoto",
        Tags = new[] { "Users" })]
    public async Task<ActionResult<BaseResponse>> UploadOwnPhoto(
        [FromForm] UploadOwnPhotoForm request,
        CancellationToken cancellationToken)
    {
        var file = request.File;
        if (file.Length <= 0 || file.Length > ProfileImageValidator.MaxBytes)
            return BadRequest(new BaseResponse { Status = false, Error = "invalid_profile_image" });

        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        var image = buffer.ToArray();
        if (!ProfileImageValidator.IsValid(image, file.ContentType))
            return BadRequest(new BaseResponse { Status = false, Error = "invalid_profile_image" });

        var saved = await userAccountService.SaveOwnPhotoAsync(
            image, file.ContentType, cancellationToken);
        return saved
            ? Ok(new BaseResponse { Status = true })
            : NotFound(new BaseResponse { Status = false });
    }

    [HttpDelete("me/photo")]
    [SwaggerOperation(
        Summary = "Remove the authenticated clinic user's profile photo",
        OperationId = "Users_DeleteOwnPhoto",
        Tags = new[] { "Users" })]
    public async Task<ActionResult<BaseResponse>> DeleteOwnPhoto(CancellationToken cancellationToken)
    {
        var removed = await userAccountService.DeleteOwnPhotoAsync(cancellationToken);
        return removed
            ? Ok(new BaseResponse { Status = true })
            : NotFound(new BaseResponse { Status = false });
    }

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
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Roles.Manage)]
    [SwaggerOperation(
        Summary = "Create a clinic user",
        Description = "Creates an ASP.NET Identity credential and matching clinic business user atomically. Creating a user also assigns protected system roles, so both user-management and role-management permissions are required. Normal users require at least one protected system role. Only a Clinic Super User can create another Super User.",
        OperationId = "Users_Create",
        Tags = new[] { "Users" })]
    public async Task<ActionResult<BaseResponse<UserAccountResponse>>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        return MapManagementResult(await userAccountService.CreateAsync(request, cancellationToken), created: true);
    }

    [HttpPut]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Users.Manage)]
    [SwaggerOperation(
        Summary = "Update a clinic user",
        Description = "Updates the user's business profile and Identity email/username in the authenticated clinic. The request body includes the user identifier. Protected Super Users cannot be modified by normal managers.",
        OperationId = "Users_Update",
        Tags = new[] { "Users" })]
    public async Task<ActionResult<BaseResponse<UserAccountResponse>>> Update(
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await updateUserValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailure<UserAccountResponse>();

        return MapManagementResult(await userAccountService.UpdateAsync(request.UserId, request, cancellationToken));
    }

    [HttpPut("roles")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Roles.Manage)]
    [SwaggerOperation(
        Summary = "Replace a clinic user's role assignments",
        Description = "Replaces the user's protected system-role assignments and revokes all active sessions so new permissions take effect immediately. The request body includes the user identifier.",
        OperationId = "Users_SetRoles",
        Tags = new[] { "Users", "RBAC" })]
    public async Task<ActionResult<BaseResponse<UserAccountResponse>>> SetRoles(
        [FromBody] SetUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await setUserRolesValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailure<UserAccountResponse>();

        return MapManagementResult(await userAccountService.SetRolesAsync(request.UserId, request, cancellationToken));
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
            return ValidationFailure<UserAccountStatusResponse>();

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

    private BadRequestObjectResult ValidationFailure<T>() where T : class => BadRequest(new BaseResponse<T>
    {
        Status = false,
        Error = "validation_error"
    });
}
