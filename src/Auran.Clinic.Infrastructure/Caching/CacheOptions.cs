namespace Auran.Clinic.Infrastructure.Caching;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public string Provider { get; set; } = CacheProviders.Memory;

    public RedisCacheOptions Redis { get; set; } = new();
}

public sealed class RedisCacheOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public string InstanceName { get; set; } = "AuranClinic:";
}

public static class CacheProviders
{
    public const string Memory = "Memory";
    public const string Redis = "Redis";
}
