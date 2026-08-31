# Backend Technical Foundation

## Goal

Build the V1 backend quickly without creating architecture that must be replaced when more clinics join the platform.

## Fixed decisions

- Solution name: `Auran.Clinic`.
- Repository: `Auran-Tech/auran-clinic-backend`.
- Runtime: .NET 10.
- Database: SQL Server.
- Persistence: Entity Framework Core.
- Architecture: layered modular monolith.
- No Shared project/layer.
- No API versioning in V1.
- Standard responses live in `Auran.Clinic.Application.Models`.
- One frontend and one backend serve multiple clinics.
- Clinic-specific behavior is configuration/data driven.
- Swagger/OpenAPI is the machine-readable API contract and uses stable operation IDs.
- API routes follow `/api/{controller}/{endpoint}`.
- `IDistributedCache` is memory-backed in V1. Redis is not an active dependency.

## Dependency direction

```text
Application    -> Domain
Infrastructure -> Application, Domain
Api            -> Application, Infrastructure
```

Controllers stay thin. Business workflows live in application/infrastructure services according to responsibility. EF Core persistence and external infrastructure concerns remain outside the Domain project.

## Platform and clinic boundary

AURAN administration and clinic operation are different security scopes:

```text
AURAN Platform
  -> PlatformUser / Platform RBAC
  -> provision clinics
  -> activate or suspend clinics
  -> control clinic features
  -> platform administrative audit

Clinic tenant
  -> clinic User / clinic RBAC
  -> clinic settings
  -> patients, queue, visits and clinical modules
  -> clinic audit
```

A Platform Admin is not a clinic Super User. A clinic Super User bypasses clinic RBAC only inside the authenticated clinic. Platform administration does not automatically grant access to patient or clinical records.

## Multi-clinic boundary

Every clinic-owned business entity inherits `ClinicEntity` and has an explicit `ClinicId`. EF Core applies a clinic query filter for an authenticated Clinic actor, and `SaveChanges` rejects writes that cross the authenticated clinic boundary. Caller-supplied tenant identifiers are never the authorization boundary.

Customer-specific code such as the following is prohibited:

```csharp
if (clinicCode == "CustomerA") { ... }
```

Differences are represented through configuration: workflow statuses, clinical fields, profile definitions, prescription sections, branding, timezone, settings and feature entitlements.

## Identity and RBAC

ASP.NET Core Identity owns credentials, password hashing, credential lockout, claims/logins and user tokens. Business authorization does not use ASP.NET Identity roles; Auran owns its Platform and Clinic role/permission models.

Permission definitions use stable underscore keys such as `Patient_View` and `Users_Manage_Status`. `Permission` stores the stable key, group key and security scope. Human descriptions live in `PermissionTranslation` rows keyed by language code. English and Arabic are provisioned initially; more languages are data additions rather than schema changes.

Normal clinic users receive the union of permissions from their assigned roles. A protected Clinic Super User receives the complete Clinic permission catalog from the backend and satisfies Clinic permission policies without frontend-side permission fabrication.

Credential lockout caused by repeated invalid passwords is an Identity security control and is separate from business account state. `User.IsActive` controls whether a clinic account is enabled. `Clinic.IsActive` controls the tenant as a whole. Login, refresh and already-issued authenticated sessions require the relevant business account and clinic to remain active.

## Clinic provisioning

A clinic is not considered created until its complete baseline has been provisioned transactionally:

```text
Platform Admin
  -> Clinic
  -> ClinicSettings
  -> default ClinicFeatures
  -> protected clinic roles
  -> role-permission mappings
  -> initial Admin Identity account
  -> domain User
  -> Admin role assignment
  -> audit
  -> commit
```

The global permission and feature catalogs are system data and are not duplicated per clinic.

## Code generation

Reusable business-number generation is centralized in `CodeCounter`. Counters are isolated by scope, optional ClinicId, code type, prefix and year. SQL Server generation uses a serializable transaction with `UPDLOCK`/`HOLDLOCK`, database unique indexes and `bigint` sequence values so callers never calculate final business identifiers in the frontend.

## Database invariants

Foundation-level invariants include:

- one queue entry per clinic visit;
- one active `VisitSession` per clinic visit;
- optimistic concurrency row versions for queue entries and visits;
- one current profile value per `(ClinicId, PatientId, FieldId)`;
- normalized multi-select profile values instead of JSON arrays;
- dedicated `FollowUp` records rather than duplicated visit follow-up text;
- clinic-aware query and write isolation.

## Feature entitlement

Feature availability is separate from RBAC. A business endpoint should be reachable only when the clinic is active, the required feature is enabled and the actor has the required clinic permission.

Feature definitions are global. `ClinicFeature` stores the enabled state and optional configuration per clinic. This is the minimum foundation for future plan/subscription mapping without introducing billing in V1.

## Audit foundation

Audit is append-only and covers automatic EF Core create/update/delete tracking plus explicit security or non-entity events. Audit records identify scope, optional clinic, actor type, actor snapshot, action, entity, metadata, network/request context and timestamp.

Sensitive values such as passwords, password hashes, JWTs, refresh tokens, signing keys, API keys, credentials, connection strings and secrets must be redacted.

Platform audit visibility deliberately excludes clinic-user clinical activity. Clinic users can only see audit records for their own clinic.

## Runtime safeguards

The API includes:

- external JWT signing-secret configuration with startup validation;
- login rate limiting;
- configured CORS allow-list support;
- centralized exception handling;
- `/health/live` process health;
- `/health/ready` SQL Server readiness;
- immediate persisted-session validation for authenticated JWT requests.

## Current implementation milestone

Before Patient Management starts, the foundation should be green and migrated with:

1. Clinic and Platform authentication.
2. Separate Platform and Clinic authorization scopes.
3. Platform Admin secure bootstrap.
4. Clinic provisioning and clinic settings.
5. Protected clinic roles, multilingual permission catalog and scoped authorization.
6. Clinic feature catalog and feature management.
7. Immediate user/clinic suspension guards.
8. Central audit trail and secret redaction.
9. EF-level clinic isolation.
10. Atomic business code generation.
11. Database concurrency/invariant constraints.
12. Stable `/api/{controller}/{endpoint}` OpenAPI contracts.
13. Unit, integration and SQL-backed flow tests.

Future subscriptions/billing, owner-portal UX, MFA, external identity, support impersonation and cross-clinic account switching remain deferred.
