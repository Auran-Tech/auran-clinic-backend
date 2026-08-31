# Caching

Auran Clinic V1 uses the ASP.NET Core `IDistributedCache` abstraction backed by in-memory caching only.

```csharp
services.AddDistributedMemoryCache();
```

Redis is intentionally **not** an active runtime dependency in V1:

- there is no Redis package reference;
- there is no Redis container in `docker-compose.yml`;
- there are no Redis connection-string settings;
- application code must not branch on a Redis provider.

This keeps the current single-instance foundation simple. Redis may be introduced later only when a measured multi-instance or distributed-cache requirement exists. That future change should remain behind `IDistributedCache` so business code does not depend on a specific cache technology.
