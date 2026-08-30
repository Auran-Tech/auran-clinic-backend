namespace Auran.Clinic.Application.Users;

public interface IUserAccountService
{
    Task<UserAccountStatusResult> SetStatusAsync(
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<UserAccountStatusResult> DisableCurrentAsync(
        CancellationToken cancellationToken = default);
}
