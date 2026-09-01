namespace Auran.Clinic.Application.Authorization;

public interface IPermissionCatalogService
{
    Task<List<PermissionCatalogResponse>> GetAsync(CancellationToken cancellationToken = default);
}
