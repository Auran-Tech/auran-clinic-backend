namespace Auran.Clinic.Application.Authorization;

public interface IEffectivePermissionService
{
    Task<IReadOnlyList<string>> GetAsync(
        bool isSuperUser,
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken = default);
}
