namespace Auran.Clinic.Application.Authorization;

public interface IRoleCatalogService
{
    Task<IReadOnlyCollection<RoleCatalogResponse>> GetAsync(
        CancellationToken cancellationToken = default);
}
