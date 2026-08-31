# DevOps Baseline

## Runtime

The solution targets .NET 10 LTS. The repository pins SDK `10.0.400` and Microsoft framework packages to the current `10.0.11` servicing release.

## CI

GitHub Actions runs on pull requests and pushes to `main`:

1. Restore
2. Build Release
3. Test
4. Publish API

## Containers

`Dockerfile` builds and runs the API using official .NET 10 images.

`docker-compose.yml` provides the local infrastructure baseline:

- API
- SQL Server 2022

Copy `.env.example` to `.env` and set a strong local SQL Server password before running:

```bash
docker compose up --build
```

Do not commit production secrets. Production connection strings, JWT keys, and other secrets must be provided by the deployment platform/environment.
