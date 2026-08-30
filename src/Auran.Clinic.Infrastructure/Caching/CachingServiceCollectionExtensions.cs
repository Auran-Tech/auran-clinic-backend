using Microsoft.Extensions.DependencyInjection;

namespace Auran.Clinic.Infrastructure.Caching;

public static class CachingServiceCollectionExtensions
{
    public static IServiceCollection AddAuranCaching(this IServiceCollection services)
    {
        services.AddDistributedMemoryCache();
        return services;
    }
}
