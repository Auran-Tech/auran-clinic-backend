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
