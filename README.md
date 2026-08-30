# Auran Clinic Backend

Backend foundation for the **Auran Clinic Management Platform** by **Auran Technology**.

Auran Clinic is one backend codebase serving multiple clinics. Clinic differences such as branding, workflow, patient profile configuration, clinical fields, clinical-order sections, timezone, localization and welcome content are configuration/data driven — never customer-specific code.

## Runtime and Architecture

- .NET 10 LTS
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity for credentials
- JWT Bearer authentication
- Permission-based Auran RBAC
- FluentValidation
- Serilog
- Swagger / OpenAPI
- xUnit
- Layered modular monolith
- No Shared project/layer
- No API version prefix in V1

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

## Multi-Clinic Boundary

Every clinic-owned business entity inherits `ClinicEntity` and carries `ClinicId`. Authenticated EF Core queries are tenant-filtered by the current request context, and writes are rejected when an entity attempts to cross the authenticated clinic boundary.

`ICurrentUserContext` is the single request-context abstraction and exposes authentication state, `UserId`, `ClinicId` and `IsSuperUser`.

A clinic can be deactivated through `Clinic.IsActive`; when inactive, login, refresh and existing JWT requests for every account in that clinic are rejected. Individual users have an explicit `User.IsActive` state and disabling a user revokes active refresh tokens.

## Permissions

Permission authorization is backend-owned. Stable keys use underscore-style identifiers such as:

```text
Patient_Create
Queue_Move
Users_Manage_Status
```

Permissions are seeded as system data. Localized descriptions live in `PermissionTranslation` rows keyed by language code. English and Arabic are seeded initially; additional languages such as German or French are data additions and do not require schema changes.

Super Users receive the complete effective permission catalog from the backend and also satisfy every permission authorization policy.

## Code Generation

Reusable sequential code generation uses `CodeCounter` with a unique `(ClinicId, CodeType, ScopeKey)` boundary and concurrency protection. For patient numbers, the year is used as the scope key so each clinic receives an independent yearly sequence.

## Caching

V1 uses `IDistributedCache` backed by in-memory caching only. Redis is intentionally not an active dependency in the current implementation and can be introduced later when a measured multi-instance use case requires it.

## API Convention

V1 endpoints follow:

```text
/api/{controller}/{endpoint}
```

Examples:

```text
POST /api/auth/login
GET  /api/auth/me
POST /api/auth/refresh
POST /api/auth/logout
GET  /api/permissions/list
PUT  /api/users/status
POST /api/users/disable-self
```

## Health

```text
GET /health/live
GET /health/ready
```

`live` confirms that the application process is running. `ready` verifies SQL Server connectivity.

## Run Locally

Prerequisites:

- .NET 10 SDK
- SQL Server

```bash
dotnet restore Auran.Clinic.sln
dotnet build Auran.Clinic.sln
dotnet test Auran.Clinic.sln
dotnet run --project src/Auran.Clinic.Api
```

## Docker Development Stack

The local compose stack contains the API and SQL Server. Redis is not part of the active V1 runtime.

```bash
cp .env.example .env
docker compose up --build
```

## CI

GitHub Actions restores, builds, runs unit and SQL Server-backed integration tests, then publishes the API for changes targeting `main`.

## Repository Rules

- No customer-specific implementations.
- No EF entities exposed directly from API endpoints.
- Thin controllers; business rules live outside controllers.
- No Shared project/layer.
- No V1 API version prefix.
- Every implemented flow requires appropriate automated tests.
- Clinic isolation and authorization are mandatory for clinic-owned functionality.

---

© Auran Technology
