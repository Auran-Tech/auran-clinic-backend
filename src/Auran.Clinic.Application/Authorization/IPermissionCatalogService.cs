namespace Auran.Clinic.Application.Authorization;

public interface IPermissionCatalogService
{
    Task<IReadOnlyList<PermissionCatalogResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
