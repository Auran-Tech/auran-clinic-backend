# Auran Clinic Backend

Backend foundation for the **Auran Clinic Management Platform** by **Auran Technology**.

The product uses one backend codebase for multiple clinics. Clinic differences such as branding, workflow, patient profile configuration, clinical fields, prescription sections, timezone, feature availability, and welcome content are driven by data and configuration — never customer-specific code branches.

## Current phase

The clinic/platform foundation is implemented and being hardened before the next patient-management feature slice.

Current foundation includes:

- .NET 10 modular-monolith solution structure.
- SQL Server + EF Core migrations.
- ASP.NET Core Identity credential storage.
- Separate Clinic and Platform authentication scopes.
- JWT access tokens with server-side revocable sessions.
- Clinic tenant query filters and write guards.
- Clinic activation/suspension enforcement.
- Clinic user activation/disable workflow.
- Clinic Super User effective permissions calculated by the backend.
- Global permission keys with multilingual `PermissionTranslation` records.
- Protected clinic roles and Platform RBAC.
- Platform Admin secure bootstrap and clinic provisioning.
- Generic concurrency-safe business-code generation.
- Memory-only caching for V1.
- File upload-session foundation.
- Central audit foundation.
- Global exception handling, validation, CORS, login rate limiting, and health checks.
- Unit, contract, and SQL-backed integration tests.

## Solution structure

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

### `Auran.Clinic.Api`

HTTP boundary only: controllers, middleware, authentication/authorization wiring, OpenAPI, DI, validation, rate limiting, and health checks.

### `Auran.Clinic.Application`

Application contracts and use-case models: service interfaces, DTOs, validators, filters, response models, authorization catalogs, and infrastructure abstractions.

### `Auran.Clinic.Domain`

Business entities, enums, constants, and domain concepts. The Domain project does not depend on ASP.NET Core or EF Core.

### `Auran.Clinic.Infrastructure`

Technical implementations: EF Core, SQL Server, Identity persistence, authentication services, tenant enforcement, business-code generation, memory caching, file storage, audit, catalog seeding, and platform/clinic services.

## Technology

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- SQL Server 2022
- ASP.NET Core Identity
- JWT Bearer Authentication
- FluentValidation
- Serilog
- Swagger / OpenAPI
- xUnit

## Security scopes

AURAN Platform administration and clinic operation are intentionally separate.

```text
Platform actor
  -> platform RBAC
  -> clinic lifecycle / features / platform audit

Clinic actor
  -> ClinicId tenant boundary
  -> clinic RBAC
  -> clinic business modules
```

A Platform Admin is not a Clinic Super User. A Clinic Super User receives all **clinic-scoped** permissions from the backend but cannot access another clinic or platform administration.

Every clinic-owned EF entity is protected by the authenticated clinic context through global query filters and write-boundary validation.

## Permissions

Permission identity is language-independent:

```text
Patient_View
Patient_Create
Users_Manage_Status
Settings_Manage
```

Localized descriptions are stored separately:

```text
Permission
  Key
  GroupKey
  Scope

PermissionTranslation
  PermissionId
  LanguageCode
  Description
```

Adding another language does not require adding database columns or changing permission keys.

## API route convention

Implemented controllers use:

```text
/api/{controller-name}/{endpoint-name}
```

Examples:

```text
POST /api/auth/login
GET  /api/auth/me
GET  /api/clinic/get-current
GET  /api/clinic/get-settings
POST /api/platform-auth/login
POST /api/platform-clinics/create
GET  /api/platform-clinics/search
GET  /api/permissions/list
```

Stable Swagger `OperationId` values are part of the API contract and are covered by automated tests.

## Run locally

Prerequisites:

- .NET 10 SDK
- SQL Server

```bash
git clone https://github.com/Auran-Tech/auran-clinic-backend.git
cd auran-clinic-backend
dotnet tool restore
dotnet restore
dotnet build
dotnet test
```

Apply migrations:

```bash
dotnet ef database update \
  --project src/Auran.Clinic.Infrastructure/Auran.Clinic.Infrastructure.csproj \
  --startup-project src/Auran.Clinic.Api/Auran.Clinic.Api.csproj
```

Run the API:

```bash
dotnet run --project src/Auran.Clinic.Api
```

Swagger is available in Development mode after startup.

## Docker development stack

The repository contains a multi-stage `Dockerfile` and `docker-compose.yml` for:

- API
- SQL Server 2022

Redis is not part of the current runtime.

```bash
cp .env.example .env
docker compose up --build
```

Set a strong local SQL Server password and JWT signing key in `.env`. Production secrets must come from the deployment environment.

## CI

Pull-request CI restores and builds the solution, applies all EF Core migrations to a clean SQL Server 2022 database, verifies that the EF model has no pending migration changes, runs automated tests (including SQL-backed integration flows), and publishes the API.

## Repository rules

- Do not add customer-specific implementations.
- Do not trust caller-supplied `ClinicId` as an authorization boundary.
- Do not expose EF entities directly from API endpoints.
- Keep controllers thin.
- Keep business logic outside controllers.
- Do not introduce a second RBAC system through ASP.NET Identity roles.
- Do not add a `Shared` project.
- Do not add API versioning in V1.
- Do not commit secrets or production connection strings.
- Every schema change requires a committed EF Core migration.
- Every implemented feature/change requires automated test coverage.

Manual endpoint testing is a final verification step after automated CI is green.

---

© Auran Technology
