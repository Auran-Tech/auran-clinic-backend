using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Auran.Clinic.Infrastructure.Platform;

public sealed class PlatformBootstrapHostedService(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var bootstrapService = scope.ServiceProvider.GetRequiredService<PlatformBootstrapService>();
        await bootstrapService.BootstrapAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
