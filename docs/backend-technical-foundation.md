# Backend Technical Foundation

## Goal

Build the V1 backend quickly without creating architecture that must be replaced when more clinics join the platform.

## Fixed Decisions

- Solution name: `Auran.Clinic`.
- Repository: `Auran-Tech/auran-clinic-backend`.
- Runtime: .NET 8.
- Database: SQL Server.
- Persistence: Entity Framework Core.
- Architecture: layered modular monolith.
- No Shared project/layer.
- No API versioning in V1.
- Standard responses live in `Auran.Clinic.Application.Models`.
- One frontend and one backend serve multiple clinics.
- Clinic-specific behavior is configuration/data driven.
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

Every clinic-owned business entity added from this point must have an explicit clinic boundary. The implementation must never contain customer-name checks such as:

```csharp
if (clinicCode == "CustomerA") { ... }
```

Instead, differences must be represented by configuration such as workflow statuses, clinical fields, profile definitions, prescription sections, branding, timezone, and settings.

## First Implementation Milestone

The first milestone should implement only:

1. Authentication.
2. Current user context.
3. Current clinic context.
4. System roles and permissions.
5. Protected Super User behavior.
6. Patient registration/search/duplicate detection.
7. Workflow statuses and transitions.
8. Live queue.
9. Visit creation and visit sessions.

After that foundation is stable, implement dynamic clinical data, prescriptions, files, follow-ups, reports, settings and audit.
