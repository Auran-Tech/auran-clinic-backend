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
- Memory cache is available for local development and Redis is supported for distributed deployments.

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

Every clinic-owned business entity has an explicit `ClinicId`. Application services must derive the tenant boundary from authenticated context rather than trusting caller-supplied tenant identifiers.

Customer-specific code such as the following is prohibited:

```csharp
if (clinicCode == "CustomerA") { ... }
```

Differences are represented through configuration: workflow statuses, clinical fields, profile definitions, prescription sections, branding, timezone, settings and feature entitlements.

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

## Feature entitlement

Feature availability is separate from RBAC. A business endpoint should be reachable only when the clinic is active, the required feature is enabled and the actor has the required clinic permission.

Feature definitions are global. `ClinicFeature` stores the enabled state and optional configuration per clinic. This is the minimum foundation for future plan/subscription mapping without introducing billing in V1.

## Audit foundation

Audit is append-only and covers automatic EF Core create/update/delete tracking plus explicit security or non-entity events. Audit records identify scope, optional clinic, actor type, actor snapshot, action, entity, metadata, network/request context and timestamp.

Sensitive values such as passwords, password hashes, JWTs, refresh tokens, signing keys, API keys, credentials, connection strings and secrets must be redacted.

Platform audit visibility deliberately excludes clinic-user clinical activity. Clinic users can only see audit records for their own clinic.

## Current implementation milestone

Before Patient Management starts, the foundation should be green and migrated with:

1. Clinic and Platform authentication.
2. Separate Platform and Clinic authorization scopes.
3. Platform Admin secure bootstrap.
4. Clinic provisioning and clinic settings.
5. Protected clinic roles and scoped permissions.
6. Clinic feature catalog and feature management.
7. Immediate clinic suspension guard.
8. Central audit trail and secret redaction.
9. Tenant-isolated audit read APIs.
10. Stable OpenAPI contracts and contract tests.

Future subscriptions/billing, owner-portal UX, MFA, external identity, support impersonation and cross-clinic account switching remain deferred.
