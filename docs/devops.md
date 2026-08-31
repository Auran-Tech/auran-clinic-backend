# DevOps Baseline

## Runtime

The backend targets **.NET 10**. The repository pins SDK `10.0.400` and the .NET 10 servicing packages used by the solution.

## CI

GitHub Actions is the automated merge gate for pull requests into `main`.

The pipeline now performs:

1. Restore local .NET tools.
2. Restore NuGet packages.
3. Build the full solution in Release mode.
4. Start SQL Server 2022 as a CI service.
5. Apply every EF Core migration to a clean SQL Server database.
6. Run `dotnet ef migrations has-pending-model-changes` so model changes without migrations fail CI.
7. Run unit and integration tests, including SQL-backed foundation flows.
8. Publish the API.

SQL-backed tests are marked with `SqlIntegrationFactAttribute` and run in CI when `AURAN_SQL_INTEGRATION=true`.

## Local containers

`docker-compose.yml` contains the local development baseline:

- API
- SQL Server 2022

Redis is intentionally **not** part of the V1 runtime. The application uses in-process memory caching only. A distributed cache can be designed later if multi-instance deployment requirements justify it.

Copy `.env.example` to `.env` and set local development secrets before starting:

```bash
cp .env.example .env
docker compose up --build
```

Required local values include a strong SQL Server SA password and a development JWT signing key.

## Database changes

Schema changes must be represented by committed EF Core migrations. Do not rely on `EnsureCreated` or manual production SQL for normal schema evolution.

Typical local commands:

```bash
dotnet tool restore

dotnet ef migrations add <MigrationName> \
  --project src/Auran.Clinic.Infrastructure/Auran.Clinic.Infrastructure.csproj \
  --startup-project src/Auran.Clinic.Api/Auran.Clinic.Api.csproj \
  --output-dir Persistence/Migrations

dotnet ef database update \
  --project src/Auran.Clinic.Infrastructure/Auran.Clinic.Infrastructure.csproj \
  --startup-project src/Auran.Clinic.Api/Auran.Clinic.Api.csproj
```

## Secrets

Do not commit production secrets. Production connection strings, JWT signing keys, bootstrap credentials, file-storage credentials, and similar values must come from the deployment environment or secret-management platform.

The first Platform Admin can be provisioned through the disabled-by-default `PlatformBootstrap` configuration. Bootstrap credentials must never be hard-coded in committed configuration.
