namespace Auran.Clinic.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(CancellationToken cancellationToken = default);
}
