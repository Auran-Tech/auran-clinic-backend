# Auran Clinic Backend

Backend foundation for the **Auran Clinic Management Platform** by **Auran Technology**.

The product is designed as one backend codebase serving multiple clinics. Clinic-specific differences such as branding, workflow, patient profile configuration, clinical fields, prescription sections, timezone, and welcome content are driven by configuration and data — never customer-specific code.

## Current Phase

**V1 backend foundation and implementation.**

The immediate implementation order is:

1. Authentication and current user context.
2. Clinic context and multi-clinic data isolation.
3. System roles, permissions, and protected Super User behavior.
4. Patients and duplicate detection.
5. Workflow configuration and live queue.
6. Visits and multi-session visits.
7. Dynamic patient profile and clinical fields.
8. Prescriptions / clinical orders, follow-ups, files, reports, settings, and audit.

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
- Repositories
- File storage
- Seed data
- External implementations

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

## Multi-Clinic Foundation

V1 contains only the foundation required to serve multiple clinics. Future SaaS concerns such as subscription billing, plan management, owner billing portal, and platform administration are intentionally not implemented now.

Current rules:

- One frontend codebase.
- One backend codebase.
- Multiple clinics.
- No clinic-specific branches in code.
- Business data must always belong to a clinic.
- Branding and configuration belong to the clinic.
- Future SaaS features must be additive, not require rewriting clinic business modules.

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

Run the API:

```bash
dotnet run --project src/Auran.Clinic.Api
```

Swagger is available in Development mode after the API starts.

## Docker Development Stack

The repository contains a multi-stage `Dockerfile` plus `docker-compose.yml` for API, SQL Server, and Redis.

```bash
cp .env.example .env
docker compose up --build
```

Set a strong local SQL Server password in `.env` before starting the stack. Production secrets must be provided by the deployment environment and must not be committed.

## CI

GitHub Actions restores, builds, tests, and publishes the API for pushes and pull requests to `main`.

## Repository Rules

- Do not add customer-specific implementations.
- Do not expose EF entities directly from API endpoints.
- Keep controllers thin.
- Business rules belong outside controllers.
- Do not add a `Shared` project.
- Do not add API versioning in V1.
- Do not commit secrets or production connection strings.

---

© Auran Technology
