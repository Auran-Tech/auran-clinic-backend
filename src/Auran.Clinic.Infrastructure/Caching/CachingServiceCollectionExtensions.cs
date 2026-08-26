using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auran.Clinic.Infrastructure.Caching;

public static class CachingServiceCollectionExtensions
{
    public static IServiceCollection AddAuranCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration[$"{CacheOptions.SectionName}:Provider"]
            ?? CacheProviders.Memory;

        if (provider.Equals(CacheProviders.Redis, StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration[$"{CacheOptions.SectionName}:Redis:ConnectionString"];
            var instanceName = configuration[$"{CacheOptions.SectionName}:Redis:InstanceName"]
                ?? "AuranClinic:";

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Cache:Redis:ConnectionString is required when Cache:Provider is Redis.");
            }

            services.AddStackExchangeRedisCache(redis =>
            {
                redis.Configuration = connectionString;
                redis.InstanceName = instanceName;
            });

            return services;
        }

        if (!provider.Equals(CacheProviders.Memory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported cache provider '{provider}'. Use Memory or Redis.");
        }

        services.AddDistributedMemoryCache();
        return services;
    }
}
