# Caching

AURAN Clinic V1 uses the ASP.NET Core distributed-memory cache implementation only.

The application registers `IDistributedCache` through `AddDistributedMemoryCache()`. Business code can depend on `IDistributedCache` without knowing the concrete implementation.

The current runtime, package graph, application configuration, and local Docker stack all use the memory-backed implementation only. Introducing a shared external cache in the future must be treated as a separate architecture decision based on deployment topology and measured load.
