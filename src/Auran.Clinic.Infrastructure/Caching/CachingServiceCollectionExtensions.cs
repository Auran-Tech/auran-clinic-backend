using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auran.Clinic.Infrastructure.Caching;

public static class CachingServiceCollectionExtensions
{
    public static IServiceCollection AddAuranCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(CacheOptions.SectionName)
            .Get<CacheOptions>() ?? new CacheOptions();

        services.AddOptions<CacheOptions>()
            .Bind(configuration.GetSection(CacheOptions.SectionName));

        if (options.Provider.Equals(CacheProviders.Redis, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.Redis.ConnectionString))
            {
                throw new InvalidOperationException(
                    "Cache:Redis:ConnectionString is required when Cache:Provider is Redis.");
            }

            services.AddStackExchangeRedisCache(redis =>
            {
                redis.Configuration = options.Redis.ConnectionString;
                redis.InstanceName = options.Redis.InstanceName;
            });

            return services;
        }

        if (!options.Provider.Equals(CacheProviders.Memory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported cache provider '{options.Provider}'. Use Memory or Redis.");
        }

        services.AddDistributedMemoryCache();
        return services;
    }
}
