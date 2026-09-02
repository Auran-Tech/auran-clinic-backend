namespace Auran.Clinic.Application.Users;

public interface IUserAccountService
{
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
