namespace Auran.Clinic.Application.Users;

public interface IUserAccountService
{
    Task<UserAccountResponse?> GetCurrentAsync(
        CancellationToken cancellationToken = default);

    Task<UserManagementResult> UpdateCurrentAsync(
        UpdateCurrentProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<UserManagementResult> ChangeOwnPasswordAsync(
        ChangeOwnPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OwnSessionResponse>> ListOwnSessionsAsync(
        Guid currentSessionId,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeOwnSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<int> RevokeAllOwnSessionsAsync(
        CancellationToken cancellationToken = default);

    Task<(byte[] ImageData, string ContentType)?> GetOwnPhotoAsync(
        CancellationToken cancellationToken = default);

    Task<bool> SaveOwnPhotoAsync(
        byte[] imageData,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteOwnPhotoAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserAccountResponse>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<UserManagementResult> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<UserManagementResult> UpdateAsync(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<UserManagementResult> SetRolesAsync(
        Guid userId,
        SetUserRolesRequest request,
        CancellationToken cancellationToken = default);

    Task<UserAccountStatusResult> SetStatusAsync(
        UpdateUserStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<UserAccountStatusResult> DisableCurrentAsync(
        CancellationToken cancellationToken = default);
}
