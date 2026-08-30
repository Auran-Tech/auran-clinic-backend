# Backend Technical Foundation

## Goal

Build Auran Clinic V1 quickly on a foundation that remains safe when multiple clinics share the same backend and database.

## Fixed Decisions

- Solution: `Auran.Clinic`.
- Runtime: .NET 10 LTS.
- Database: SQL Server.
- Persistence: Entity Framework Core.
- Architecture: layered modular monolith.
- No Shared project/layer.
- No API version prefix in V1.
- API route convention: `/api/{controller}/{endpoint}`.
- One frontend and one backend serve multiple clinics.
- Clinic-specific behavior is configuration/data driven.
- Redis is deferred; the active cache implementation is distributed in-memory caching.
- Future subscriptions, billing, owner portal and platform administration remain outside V1.

## Dependency Direction

```text
Application    -> Domain
Infrastructure -> Application, Domain
Api            -> Application, Infrastructure
```

## Current Request Context

`ICurrentUserContext` is the single request-context abstraction:

```text
IsAuthenticated
UserId
ClinicId
IsSuperUser
```

JWT claims provide the authenticated values. Client-supplied clinic identifiers are never an authorization boundary.

## Multi-Clinic Isolation

Clinic-owned entities inherit `ClinicEntity` and carry `ClinicId`.

The persistence boundary uses defense in depth:

1. JWT contains `clinic_id`.
2. `ICurrentUserContext` resolves the authenticated clinic.
3. EF Core global query filters scope clinic-owned reads.
4. `SaveChanges` rejects cross-clinic writes and automatically sets an empty `ClinicId` on new authenticated entities.
5. Services still validate aggregate-specific ownership where required.

Pre-authentication flows such as login and refresh may use `IgnoreQueryFilters()` only when they explicitly validate clinic ownership and active state themselves.

## Account State

`User.IsActive` controls an individual clinic account. `Clinic.IsActive` controls the entire clinic. Login, refresh and JWT validation all enforce both states, so deactivation invalidates existing authenticated traffic immediately.

Account-state changes are explicit business operations. They are not implemented as automatic failed-password business locks.

## Permissions

Auran RBAC is independent of ASP.NET Core Identity roles. Identity owns credentials; Auran owns `Role`, `Permission`, `UserRole` and `RolePermission`.

Permission keys are stable backend identifiers such as `Patient_Create` and `Queue_Move`. Human descriptions are stored separately in `PermissionTranslation` by language code. English and Arabic are initially seeded; additional languages are data-only additions.

A Super User:

- is still restricted to its clinic;
- satisfies every backend permission policy;
- receives every effective permission key from the backend response.

## Persistence Invariants

- one current `QueueEntry` per `Visit`;
- SQL Server rowversion on `QueueEntry` and `Visit`;
- only one active `VisitSession` per visit;
- normalized multi-select patient-profile values;
- `FollowUp` is the follow-up source of truth;
- clinic logos reference `FileRecord` metadata;
- reusable `CodeCounter` generates clinic/scoped sequences transactionally.

## Security Baseline

- HTTPS redirection;
- JWT signature/lifetime validation;
- active user and clinic validation on JWT requests;
- permission authorization;
- clinic query/write isolation;
- FluentValidation request validation;
- CORS whitelist;
- login rate limiting;
- global exception handling;
- liveness and SQL readiness health checks.

## Testing Baseline

Completed foundation behavior must have automated tests. CI runs .NET 10 restore/build/test/publish and provides SQL Server for integration tests. Priority coverage includes authentication, current context, Super User effective permissions, permission denial, account/clinic deactivation, tenant isolation, code generation and persistence concurrency/invariants.
