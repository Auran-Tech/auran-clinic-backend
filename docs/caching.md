# Cache Provider Configuration

The backend exposes one distributed-cache abstraction and chooses the implementation from configuration.

## Memory

Use for local development or single-instance environments:

```json
"Cache": {
  "Provider": "Memory"
}
```

## Redis

Use for production or multiple API instances:

```json
"Cache": {
  "Provider": "Redis",
  "Redis": {
    "ConnectionString": "redis:6379",
    "InstanceName": "AuranClinic:"
  }
}
```

Recommended production configuration is through environment variables rather than committed secrets:

```text
Cache__Provider=Redis
Cache__Redis__ConnectionString=<redis-connection-string>
Cache__Redis__InstanceName=AuranClinic:
```

The application receives `IDistributedCache` regardless of provider, so business code does not need Memory/Redis-specific branches.
