using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Auran.Clinic.Infrastructure.Authorization;

public sealed class PermissionCatalogHostedService(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<PermissionCatalogInitializer>();
        await initializer.InitializeAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
