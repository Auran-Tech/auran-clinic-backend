# Caching

Auran Clinic keeps the `IDistributedCache` abstraction available, but the active V1 implementation is **in-memory only**.

```csharp
services.AddDistributedMemoryCache();
```

Redis is not part of the active runtime, NuGet graph, production configuration or Docker Compose stack.

## Why

The current foundation does not yet have a measured multi-instance use case that requires a remote distributed cache. Keeping Redis active before a real requirement would add deployment and operational complexity without providing current product value.

## Future Redis Adoption

Redis may be introduced later when there is a concrete requirement such as multiple API instances sharing cache state or measured cache-heavy workloads. That change must be explicit and should include:

- package/runtime dependency;
- secret-managed connection configuration;
- cache key conventions including clinic scope;
- invalidation strategy;
- resilience behavior when Redis is unavailable;
- integration/load tests.

Business code should continue depending on cache abstractions rather than Redis-specific APIs.
