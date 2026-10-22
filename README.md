# Auran Clinic Backend

Backend foundation for the **Auran Clinic Management Platform** by **Auran Technology**.

The product is designed as one backend codebase serving multiple clinics. Clinic-specific differences such as branding, workflow, patient profile configuration, clinical fields, prescription sections, timezone, and welcome content are driven by configuration and data — never customer-specific code.

## Current Phase

**V1 backend foundation complete and ready for review/testing.**

The foundation now includes:

1. Clinic and Platform authentication with separate actor boundaries.
2. Current-user and clinic context.
3. Fail-closed multi-clinic query/write isolation for authenticated actors.
4. Protected system roles, permission catalog, and Clinic Super User behavior.
5. Clinic user lifecycle and role assignment.
6. Platform Admin bootstrap and authentication.
7. Platform clinic provisioning, metadata management, activation, and suspension.
8. Refresh-token rotation, session revocation, account/clinic state validation, and login rate limiting.
9. Audit logging for sensitive clinic-user management actions.
10. Health checks, structured request logging, correlation IDs, Docker development setup, migrations, and CI quality gates.

After foundation review and testing, the next business implementation stage starts with **Patients and duplicate detection**.

## Solution Structure

```text
Auran.Clinic.sln

src/
  Auran.Clinic.Api/
  Auran.Clinic.Application/
  Auran.Clinic.Domain/
  Auran.Clinic.Infrastructure/

tests/
  Auran.Clinic.UnitTests/
  Auran.Clinic.IntegrationTests/
```

There is intentionally **no Shared project or Shared layer**.

### Auran.Clinic.Api

HTTP boundary only:

- Controllers
- Middleware
- Authentication wiring
- Authorization wiring
- Swagger / OpenAPI
- Dependency injection
- Health checks

### Auran.Clinic.Application

Application use cases and service contracts:

- Services
- DTOs
- Validators
- Filters
- Application models
- Persistence/auth/storage abstractions

### Auran.Clinic.Domain

Business model and business rules:

- Entities
- Enums
- Constants
- Domain policies
- Domain exceptions

The Domain project does not depend on ASP.NET Core or EF Core.

### Auran.Clinic.Infrastructure

Technical implementations:

- Entity Framework Core
- SQL Server
- Identity persistence
- Authentication/session persistence
- Authorization and permission catalog initialization
- Clinic provisioning
- Audit persistence
- Caching and code generation
- Seed/bootstrap services

## Technology

- .NET 10 LTS
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity
- JWT Bearer Authentication
- FluentValidation
- Serilog
- Swagger / OpenAPI
- xUnit

## Actor and Tenant Boundaries

The runtime has two authenticated actor types:

- **Clinic** — must carry a valid `clinic_id` and can access only that clinic's protected APIs/data.
- **Platform** — can access only Platform APIs by default. Clinic-owned EF entities remain hidden unless a Platform infrastructure operation explicitly enters a clinic scope.

Authenticated clinic-owned data access is **fail closed**. A valid authenticated token without an effective clinic scope does not see clinic-owned rows and cannot write them.

Unauthenticated/system scopes remain available for narrowly defined startup, authentication, migration, and bootstrap operations.

## Protected System Roles

V1 uses four backend-owned system roles:

- `ADMIN`
- `RECEPTIONIST`
- `DOCTOR`
- `NURSE`

The catalog and its default permission mappings are initialized by the backend. These roles are protected definitions; clinic administrators assign them to users rather than renaming or deleting them.

A Clinic **Super User** remains a protected business flag and receives all known permissions independent of normal role assignments. The final active Super User in a clinic cannot be deactivated.

## Platform Clinic Provisioning

A Platform actor can provision and manage clinics through `/api/platform/clinics`.

Provisioning is atomic and creates:

- The clinic.
- A server-generated clinic code.
- Default clinic settings.
- The first Clinic Identity account.
- The first active Clinic Super User.
- The initial `ADMIN` role assignment.

Suspending a clinic immediately invalidates its active clinic sessions because access-token validation checks the persisted clinic state on authenticated requests.

## Standard Service Responses

Application services use the agreed response models in `Auran.Clinic.Application.Models`.

```csharp
public class BaseResponse
{
    public string? Message { get; set; }
    public bool Status { get; set; }
    public string? Error { get; set; }
}

public class BaseResponse<T> : BaseResponse where T : class
{
    public T? Data { get; set; }
}
```

Pagination uses one standard application model:

```csharp
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new List<T>();
    public required PaginationInfo Setting { get; set; }
}
```

## Run Locally

### Docker MVP 1 test stack

From this backend directory, with the frontend checkout beside it:

1. Copy `.env.example` to `.env` and replace the SQL, JWT, and one-time Platform bootstrap passwords with strong local values. Set `AURAN_FRONTEND_PATH` if the frontend checkout has a different sibling directory name. `.env` is ignored by Git.
2. Run `docker compose up -d --build`. Compose starts SQL Server, applies the EF migrations in a one-shot container, then starts the API and the built frontend. The API stores uploads in a separate persistent Docker volume.
3. Check `docker compose ps`, `http://127.0.0.1:5085/health/ready`, and `http://127.0.0.1:4173/login`. The Platform UI is at `http://127.0.0.1:4173/platform`; use the bootstrap account to create a test clinic and its first Clinic Super User.
4. After the first Platform account exists, set `PLATFORM_BOOTSTRAP_ENABLED=false` and remove the bootstrap password from `.env`, then run `docker compose up -d` again. Keep test credentials outside Git.
5. For browser regression tests from the frontend directory, set `E2E_API_BASE_URL=http://127.0.0.1:5085/api`, `E2E_REUSE_EXISTING_SERVER=true`, `PlatformBootstrap__Email`, and `PlatformBootstrap__Password` to the local test account values, then run `npx playwright test --workers=4`. The live tests create synthetic clinics and records, so use an isolated test stack rather than a clinic used for manual testing.

The frontend Nginx image supports an internal `/api` proxy; this local Compose build points the browser directly at `127.0.0.1:5085` for the existing live-stack tests. Host ports are bound to localhost: frontend `4173`, API `5085`, SQL Server `14330`. `docker compose down` stops the stack and retains SQL and uploads; `docker compose down -v` removes both volumes and all test data. Use synthetic data only. This is a local Development setup, not a production deployment.

### Share the running local MVP with developers

Start the Docker MVP stack above first. Keep Docker Desktop and this laptop running. From this backend directory in PowerShell, start a public temporary link:

```powershell
.\scripts\start-local-share.ps1
```

The script rebuilds the frontend with same-origin `/api` and `/health` routing, checks API readiness, and prints a random `trycloudflare.com` URL. Anyone with the URL can open the Auran sign-in page; share test account credentials only with the intended developers. Press Ctrl+C to stop sharing. If the tunnel was started separately in detached Docker mode, run `docker compose -f docker-compose.yml -f docker-compose.share.yml --profile share stop share` to stop it. The URL also stops working if the laptop, Docker, or tunnel stops; a later run gets a new URL. The backend and SQL Server host ports remain bound to localhost. Use only synthetic test data and test accounts. This is a temporary development link, not production hosting. The [Cloudflare Quick Tunnel documentation](https://developers.cloudflare.com/tunnel/get-started/quick-tunnels/) describes the URL lifetime.

Prerequisites:

- .NET 10 SDK
- SQL Server

Clone and build:

```bash
git clone https://github.com/Auran-Tech/auran-clinic-backend.git
cd auran-clinic-backend
dotnet restore
dotnet build
dotnet test
```

A database connection string and JWT signing key are required at runtime. Production values must come from the deployment environment or secret store.

Run the API:

```bash
ConnectionStrings__DefaultConnection="<connection-string>" \
Jwt__SigningKey="<at-least-32-random-characters>" \
dotnet run --project src/Auran.Clinic.Api
```

Swagger UI is available in Development mode after the API starts. The machine-readable OpenAPI document remains available in all environments by design.

## Docker Development Stack

The repository contains a multi-stage `Dockerfile` plus `docker-compose.yml` for the API and SQL Server.

```bash
cp .env.example .env
docker compose up --build
```

Set strong local SQL Server and JWT values in `.env` before starting the stack.

To create the first Platform Admin, temporarily set the `PLATFORM_BOOTSTRAP_*` values in `.env` and set `PLATFORM_BOOTSTRAP_ENABLED=true`. After the first Platform account is successfully created, disable the bootstrap and remove the one-time password from the local environment.

Production secrets must be supplied by the deployment environment and must not be committed.

## Observability and Health

- `GET /health/live` — process liveness.
- `GET /health/ready` — readiness including database connectivity.
- Every HTTP response receives `X-Correlation-ID` using the ASP.NET Core trace identifier.
- Serilog request logging includes the same trace identifier for correlation.

## CI

GitHub Actions runs for pull requests and `main` and performs:

1. Restore.
2. Release build with warnings treated as errors.
3. Apply EF migrations to a real SQL Server container.
4. Verify the EF model has no pending migration changes.
5. Run unit and integration tests.
6. Publish the API.

## Repository Rules

- Do not add customer-specific implementations.
- Do not expose EF entities directly from API endpoints.
- Keep controllers thin.
- Business rules belong outside controllers.
- Clinic-owned data must stay tenant scoped.
- Platform code must explicitly enter a clinic scope before accessing clinic-owned entities.
- Do not add a `Shared` project.
- Do not add API versioning in V1.
- Do not commit secrets or production connection strings.

---

© Auran Technology
