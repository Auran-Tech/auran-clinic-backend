# AURAN Clinic — Consolidated Backend Specification

_Last updated: 2026-08-31_

This document is the current authoritative backend foundation reference. When an older document, comment, or prototype note conflicts with this file and the implemented code, this file describes the intended current foundation behavior.

## 1. Product and architecture

AURAN Clinic is a multi-tenant clinic-management product using one frontend codebase and one backend codebase. Clinic differences are represented by configuration and data, not customer-specific code copies.

Backend stack:

- .NET 10
- ASP.NET Core Web API
- SQL Server 2022
- Entity Framework Core 10
- ASP.NET Core Identity
- JWT Bearer authentication
- FluentValidation
- Serilog
- Swagger/OpenAPI
- in-process memory caching
- xUnit
- modular monolith architecture

Projects:

```text
src/Auran.Clinic.Api
src/Auran.Clinic.Application
src/Auran.Clinic.Domain
src/Auran.Clinic.Infrastructure

tests/Auran.Clinic.UnitTests
tests/Auran.Clinic.IntegrationTests
```

There is no Shared project/layer.

## 2. Security scopes

Platform and Clinic actors are separate security scopes.

### Platform actor

Represents an AURAN-side operator. Platform actors manage tenant lifecycle, feature entitlement, and platform-visible audit information.

A Platform Admin does not automatically gain clinical-data access.

### Clinic actor

Represents a user inside exactly one clinic. Clinic users are restricted to their authenticated `ClinicId`.

A Clinic Super User:

- belongs to one clinic,
- receives all clinic-scoped permissions from the backend,
- bypasses ordinary clinic RBAC only inside that clinic,
- does not become a Platform Admin,
- cannot access another clinic.

## 3. Identity and account state

ASP.NET Core Identity is the credential store.

```text
ApplicationIdentityUser(AccountType=Platform) -> PlatformUser
ApplicationIdentityUser(AccountType=Clinic)   -> User -> Clinic
```

Identity lockout and business account state are different controls:

- Identity lockout protects against repeated authentication failures.
- `User.IsActive` enables/disables a clinic business account.
- `Clinic.IsActive` enables/suspends the complete tenant.

Suspending a clinic blocks all clinic accounts, including requests using previously issued access tokens.

## 4. Current actor context and tenant isolation

`ICurrentActor` is the single authenticated-actor abstraction.

It exposes actor type, Identity id, Platform/Clinic domain ids, ClinicId, Super User state, display name, and email.

Clinic isolation is enforced centrally by EF Core:

1. Global query filters restrict `ClinicEntity` queries to the authenticated clinic.
2. SaveChanges guards reject cross-clinic create/update/delete attempts.
3. `ClinicId` is not accepted from normal clinic-facing requests as an authorization boundary.

Platform/system operations use their explicit non-clinic actor scope.

## 5. Authentication and revocable sessions

Clinic and Platform authentication use separate endpoints and domain session records.

JWTs contain an explicit actor type and a `session_id`.

Clinic JWT claims conceptually include:

```text
actor_type=Clinic
clinic_user_id
clinic_id
clinic_super_user
clinic_role
clinic_permission
session_id
```

Platform JWT claims conceptually include:

```text
actor_type=Platform
platform_user_id
platform_role
platform_permission
session_id
```

Every protected request validates both the JWT and the persisted session state.

Session lifecycle:

```text
Login
  -> AccessToken1 + RefreshToken1 + Session1

Refresh
  -> atomically revoke Session1
  -> issue AccessToken2 + RefreshToken2 + Session2
  -> old access token becomes invalid

Logout
  -> revoke current session
  -> current access token becomes invalid
```

Refresh tokens are cryptographically random. Only SHA-256 hashes are stored.

Clinic login/refresh also reject inactive users, inactive clinics, and Identity lockout where applicable.

## 6. Authorization and permissions

Permission keys are stable, language-independent identifiers.

Examples:

```text
Patient_View
Patient_Create
Patient_Edit_Basic
Users_View
Users_Manage
Users_Manage_Status
RBAC_View
RBAC_Manage
Queue_View
Queue_Move
Visit_View
Visit_Start
Visit_Edit
MedicalProfile_View
MedicalProfile_Edit
FollowUp_View
FollowUp_Manage
Reports_View
Reports_Export
Settings_View
Settings_Manage
Files_View
Files_Upload
```

Platform permission keys use the same underscore convention, for example:

```text
Platform_Clinics_View
Platform_Clinics_Create
Platform_Clinics_Set_Status
Platform_Clinics_Manage_Features
```

Persistence:

```text
Permission
  Id
  Key
  GroupKey
  Scope

PermissionTranslation
  Id
  PermissionId
  LanguageCode
  Description
```

English (`en`) and Arabic (`ar`) descriptions are seeded. German, French, or other languages can be added as translation rows without schema changes.

Normal users receive the union of permissions assigned through their roles. Clinic Super Users receive every clinic-scoped permission from the backend itself.

## 7. Roles

Clinic protected roles:

- Admin
- Receptionist
- Doctor
- Nurse

Roles are clinic-scoped. Users can have multiple roles.

ASP.NET Identity roles are not used as a second application RBAC system. The persistence context uses the user-only Identity model while AURAN domain roles/permissions remain the business authorization model.

## 8. Account status management

Administrative clinic-account state changes require `Users_Manage_Status` or protected Super User behavior according to the service rules.

A user can disable their own account through the explicit self-disable endpoint.

Disabling a user revokes refresh sessions and protected access is rejected immediately.

A normal manager cannot disable a protected Clinic Super User in violation of the service rules.

## 9. Platform bootstrap

There is no public Platform Admin registration endpoint.

Initial Platform Admin bootstrap uses secure deployment configuration:

```text
PlatformBootstrap__Enabled
PlatformBootstrap__FullName
PlatformBootstrap__Email
PlatformBootstrap__Password
PlatformBootstrap__Phone
```

Bootstrap is disabled by default and idempotently ensures:

- permission catalog,
- translation catalog,
- feature catalog,
- protected `PLATFORM_ADMIN` role,
- Platform role-permission mappings,
- Identity user,
- `PlatformUser`,
- role assignment,
- audit history.

No real bootstrap credentials are committed.

## 10. Clinic provisioning

Clinic provisioning is a transaction:

```text
Platform Admin
  -> validate request
  -> generate Clinic.Code
  -> create Clinic
  -> create ClinicSettings
  -> create default ClinicFeatures
  -> create protected clinic roles
  -> assign default role permissions
  -> create initial Admin Identity account
  -> create clinic User (active)
  -> assign Admin role
  -> write audit
  -> commit
```

The caller provides `CodePrefix`, not final `Code`.

The initial clinic Admin is a normal Admin-role user, not automatically a Clinic Super User.

## 11. Business-code generation

Business identifiers are generated by the backend.

Format:

```text
PREFIX-YEAR-SEQUENCE
```

Examples:

```text
CDC-2026-1
PT-2026-1
```

`CodeCounter` supports Platform and Clinic scopes and currently includes Clinic and Patient code types.

Counter increments are implemented with SQL Server locking/transaction semantics and protected unique indexes so concurrent requests cannot generate duplicate sequence values.

System role/permission/feature keys are constants and are not generated through `CodeCounter`.

## 12. Database invariants

Foundation invariants include:

- unique `(ClinicId, PatientNumber)`,
- unique `(ClinicId, Phone)`,
- one queue entry per `(ClinicId, VisitId)`,
- one active visit session per `(ClinicId, VisitId)` where `EndedAtUtc IS NULL`,
- one current patient-profile value per `(ClinicId, PatientId, FieldId)`,
- normalized `PatientProfileValueOption` rows for multi-select values,
- SQL `rowversion` concurrency on queue entries and visits,
- CodeCounter scope consistency check constraint and concurrency-safe unique indexes,
- unique `(PermissionId, LanguageCode)` translation rows.

The dedicated `FollowUp` entity is the source of truth for follow-up data; duplicate visit follow-up text storage was removed.

## 13. Dynamic patient profile

Patient basic identity remains on `Patient`.

Specialty-specific fields use configuration tables:

```text
PatientProfileSection
PatientProfileField
PatientProfileFieldOption
PatientProfileValue
PatientProfileValueOption
```

Typed profile values are used for text/number/boolean/date/file values. Multi-select values use normalized option-link rows rather than JSON arrays.

## 14. Files

Business entities should store stable `FileId` references rather than permanent storage URLs wherever possible.

Upload flow:

```text
create upload session
  -> return short-lived UploadUrl
  -> upload raw bytes
  -> complete upload session
  -> create permanent FileRecord
  -> return FileId + metadata + current URL
```

Upload tokens are short-lived scoped credentials and are stored only as hashes.

Local storage is implemented behind a storage-provider abstraction. S3/direct-storage behavior can be added later without changing business file references.

## 15. Audit

Audit is append-only.

Automatic EF auditing captures entity mutations. Explicit events capture authentication, refresh/logout, authorization denial, provisioning, status changes, feature changes, file downloads, and other non-CRUD operations.

Audit actor snapshots preserve identity context historically.

Sensitive values such as passwords, JWTs, refresh tokens, signing keys, API keys, credentials, and connection strings must be redacted.

## 16. Caching

V1 uses **memory caching only**.

Redis support, Redis packages, Redis runtime configuration, and the Redis docker service are intentionally removed.

A distributed-cache strategy may be introduced later if multi-instance deployment requires it.

## 17. API route convention

Implemented controller routes follow:

```text
/api/{controller-name}/{endpoint-name}
```

Examples:

```text
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
GET  /api/auth/me

POST /api/platform-auth/login
POST /api/platform-auth/refresh
POST /api/platform-auth/logout

GET  /api/platform-clinics/search
POST /api/platform-clinics/create
GET  /api/platform-clinics/get/{id}
PUT  /api/platform-clinics/update/{id}
PUT  /api/platform-clinics/set-status/{id}
GET  /api/platform-clinics/get-features/{id}
PUT  /api/platform-clinics/update-features/{id}

GET  /api/clinic/get-current
GET  /api/clinic/get-settings
PUT  /api/clinic/update-settings
GET  /api/clinic/get-features

GET  /api/permissions/list
PUT  /api/users/status
POST /api/users/disable-self
```

Reference-data endpoints follow the same controller/action pattern under `/api/reference-data/...`.

Stable Swagger `OperationId` values are contract-tested.

## 18. HTTP/security middleware

The API foundation includes:

- global exception handling,
- standardized API response failures,
- CORS allow-list configuration,
- login rate limiting,
- forwarded-header support,
- JWT validation,
- structured authentication/authorization failures,
- `/health/live`,
- `/health/ready` with database readiness checking,
- Swagger/OpenAPI.

Production signing keys and secrets come from deployment configuration and are not committed.

## 19. Testing and CI

Every implemented change requires automated test coverage appropriate to its level.

CI performs:

1. restore,
2. Release build,
3. SQL Server 2022 startup,
4. migration application to a clean database,
5. EF pending-model-change verification,
6. unit tests,
7. integration/contract tests,
8. SQL-backed foundation tests,
9. API publish.

SQL-backed automated coverage includes:

- Platform bootstrap,
- Platform login,
- clinic provisioning,
- Clinic login/current-user flow,
- multilingual permission catalog,
- refresh rotation and old-session rejection,
- logout revocation,
- clinic suspension/resume,
- Clinic Super User full effective permissions,
- self-disable behavior,
- tenant query isolation,
- cross-clinic write rejection,
- concurrent CodeCounter generation.

Manual endpoint testing is intentionally left as the final verification activity after automated CI is green.

## 20. Deferred work

The foundation intentionally does not pre-build every future SaaS/product concern.

Deferred until explicitly designed or required:

- subscription billing and plans,
- owner billing portal,
- cross-clinic account switching,
- MFA and external identity providers,
- support impersonation/break-glass clinical access,
- distributed Redis caching,
- appointments/branches unless promoted into an active feature scope,
- future external lab/radiology/pharmacy integrations.

## 21. Merge position of PR #5 and PR #6

PR #6 (`fix/foundation-hardening`) is the continuation/superset of the clinic-management/audit foundation work from PR #5. The hardening branch contains the PR #5 foundation plus the security, data-model, migration, permission, route, caching, and automated-test corrections described here.

The PRs must not both be merged independently when that would duplicate the same work. PR #6 is the branch intended to carry the final hardened foundation once CI and review gates are satisfied.
