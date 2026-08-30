# DevOps Baseline

## Runtime

The solution targets **.NET 10 LTS**. The repository pins SDK `10.0.400` and Microsoft framework packages to the .NET 10 servicing line.

## CI

GitHub Actions runs for pull requests and pushes targeting `main`:

1. Start SQL Server 2022 for integration tests.
2. Restore.
3. Build Release.
4. Run unit and SQL Server-backed integration tests.
5. Publish the API.

A green pipeline is required before foundation work is considered complete.

## Containers

`Dockerfile` builds and runs the API using official .NET 10 images.

`docker-compose.yml` provides the local V1 infrastructure baseline:

- API
- SQL Server 2022

Redis is intentionally not part of the active V1 stack.

```bash
cp .env.example .env
docker compose up --build
```

## Health

- `/health/live` confirms the API process is alive.
- `/health/ready` verifies that SQL Server is reachable.

## Configuration

Environment-specific configuration should be supplied by the deployment platform. CORS origins are whitelisted through configuration. Production infrastructure and secret values must not be hard-coded into deployment definitions.
