namespace Auran.Clinic.Application.Users;

public interface IUserAccountService
{
    Task<UserAccountStatusResult> SetStatusAsync(
        UpdateUserStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<UserAccountStatusResult> DisableCurrentAsync(
        CancellationToken cancellationToken = default);
}
