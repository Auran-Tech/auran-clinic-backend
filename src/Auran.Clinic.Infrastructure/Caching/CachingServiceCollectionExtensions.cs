using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auran.Clinic.Infrastructure.Caching;

public static class CachingServiceCollectionExtensions
{
    public static IServiceCollection AddAuranCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();

        if (options.Provider.Equals(CacheProviders.Memory, StringComparison.OrdinalIgnoreCase))
        {
            services.AddDistributedMemoryCache();
            return services;
        }

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

        throw new InvalidOperationException(
            $"Unsupported cache provider '{options.Provider}'. Supported providers are Memory and Redis.");
    }
}
