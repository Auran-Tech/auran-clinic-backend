# Backend Technical Foundation

## Goal

Build the V1 backend quickly without creating architecture that must be replaced when more clinics join the platform.

## Fixed Decisions

- Solution name: `Auran.Clinic`.
- Repository: `Auran-Tech/auran-clinic-backend`.
- Runtime: .NET 10 LTS.
- Database: SQL Server.
- Persistence: Entity Framework Core.
- Architecture: layered modular monolith.
- No Shared project/layer.
- No API versioning in V1.
- Standard responses live in `Auran.Clinic.Application.Models`.
- One frontend and one backend serve multiple clinics.
- Clinic-specific behavior is configuration/data driven.
- API routes follow `/api/controllername/endpointname`.
- ASP.NET Core Identity owns credential/password persistence only; Auran domain RBAC is the source of truth for application roles and permissions.
- Permission keys are stable backend identifiers using underscore notation such as `Attendance_Create_Shift`.
- Permission descriptions are localized data (`PermissionTranslation`) rather than part of the authorization key.
- Super Users receive all known effective permission claims from the backend; authorization does not bypass permission policies based on a Super User flag.
- Business account activation/deactivation is separate from ASP.NET Identity failed-login lockout.
- Current V1 caching uses the in-process distributed-memory implementation only. Any shared external cache is a future architecture decision.
- Future subscriptions, billing, owner portal and platform administration are out of scope for V1.

## Dependency Direction

```text
Api ------------> Application
 |                    |
 |                    v
 +------------> Infrastructure ----> Domain
                      |
Application ----------+
      |
      v
    Domain
```

Actual project references are intentionally simpler:

```text
Application    -> Domain
Infrastructure -> Application, Domain
Api            -> Application, Infrastructure
```

## Multi-Clinic Boundary

Every clinic-owned business entity must have an explicit clinic boundary. The implementation must never contain customer-name checks such as:

```csharp
if (clinicCode == "CustomerA") { ... }
```

Instead, differences must be represented by configuration such as workflow statuses, clinical fields, profile definitions, prescription sections, branding, timezone, and settings.

The current persistence boundary includes clinic query filters and write-time tenant checks. SQL-level tenant relationship invariants should also be used wherever a clinic-owned child references a clinic-owned parent so database integrity does not depend only on application code.

## Authentication and authorization baseline

- `POST /api/auth/login` authenticates a clinic user and creates an access/refresh session.
- `POST /api/auth/refresh` rotates a refresh token atomically.
- `POST /api/auth/logout` revokes the authenticated user's supplied refresh token.
- `GET /api/permissions/list` exposes the stable permission catalog and all persisted localized descriptions.
- `PUT /api/users/status` changes a clinic user's business account status and requires `Users_Manage_Status`.
- `POST /api/users/disable-self` allows an authenticated user to disable their own business account.
- Disabling a user revokes active refresh sessions; existing JWT access is rejected because token validation checks the active user, clinic, and session state.
- Disabling a clinic causes existing clinic JWTs to fail active-state validation.

## Current implementation milestone

The foundation currently covers:

1. Authentication and rotating refresh sessions.
2. Current user and clinic context.
3. Multi-clinic EF query/write boundaries.
4. Stable localized permission catalog.
5. Domain RBAC and backend-issued effective permission claims.
6. Protected Super User behavior without authorization bypass.
7. Business account status and immediate session invalidation.
8. SQL migrations and SQL-backed integration flows in CI.
9. Live/readiness health checks, login rate limiting, CORS, exception handling, and secret validation.

The next domain implementation milestones build on this foundation: patient registration/search/duplicate detection, workflow statuses/transitions, live queue, visits, dynamic clinical data, files, follow-ups, reports, settings, and audit behavior.
