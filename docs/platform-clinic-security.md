# Platform, Clinic Provisioning, Features & Audit

## Security scopes

AURAN Clinic separates platform administration from clinic operation. The scopes share ASP.NET Core Identity only as a credential store; they use different domain users, RBAC graphs, JWT claims, and authorization policies.

```text
Identity(AccountType=Platform)
  -> PlatformUser
  -> PlatformRole / PlatformPermission

Identity(AccountType=Clinic)
  -> User + ClinicId
  -> Role / ClinicPermission
```

A Platform Admin does not implicitly enter a clinic's clinical scope. A Clinic Super User bypasses clinic RBAC only inside that user's own active clinic.

## Identity versus business account state

Identity lockout and business account activation are deliberately different concepts.

- Identity lockout protects authentication from repeated invalid-password attempts.
- `User.IsActive` controls whether a clinic account is enabled for business access.
- `Clinic.IsActive` controls whether the entire tenant can authenticate or continue using existing sessions.

Disabling a clinic blocks all clinic accounts. Disabling a clinic user revokes that user's refresh sessions and subsequent protected requests are rejected.

## Platform bootstrap

There is no public Platform Admin registration endpoint.

The initial Platform Admin is created through disabled-by-default deployment configuration:

```text
PlatformBootstrap__Enabled
PlatformBootstrap__FullName
PlatformBootstrap__Email
PlatformBootstrap__Password
PlatformBootstrap__Phone
```

Bootstrap is idempotent and ensures the global permission/feature catalogs, protected `PLATFORM_ADMIN` role, role permissions, Identity account, `PlatformUser`, assignment, and audit record.

## Clinic provisioning

Only an authenticated platform actor with `Platform_Clinics_Create` can provision a clinic.

Provisioning is transactional:

```text
validate request
  -> generate clinic business code
  -> create Clinic
  -> create ClinicSettings
  -> create default ClinicFeatures
  -> create protected Admin/Receptionist/Doctor/Nurse roles
  -> map role permissions
  -> create initial Clinic Identity account
  -> create domain User (IsActive=true)
  -> assign Admin role
  -> audit
  -> commit
```

The generated clinic code is immutable. The API receives a `CodePrefix`, not a caller-generated final code.

## Permissions

Permission identity is stable and language-independent:

```text
Platform_Clinics_Create
Patient_View
Users_Manage_Status
Settings_Manage
```

Persistence uses:

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

English and Arabic are seeded now. Additional languages can be added as translation rows without schema changes.

A Clinic Super User's effective permission list is calculated by the backend as **all clinic-scoped permissions**. The frontend does not invent or bypass permission decisions.

## Protected clinic roles

Every clinic starts with:

- Admin
- Receptionist
- Doctor
- Nurse

Roles are clinic-scoped and unique by `(ClinicId, Code)`. Users may have multiple roles and receive the union of their role permissions.

`Users_Manage_Status` controls administrative activation/deactivation of clinic accounts. A user may disable their own account through the dedicated self-disable operation. Protected Super User restrictions are enforced by the backend service.

## Tenant isolation

Clinic-owned entities inherit `ClinicEntity` and carry `ClinicId`.

The EF Core context enforces the tenant boundary in two directions:

1. Global query filters hide rows belonging to other clinics for authenticated clinic actors.
2. `SaveChanges` write guards reject attempts to create, modify, or delete clinic-owned rows outside the authenticated `ClinicId`.

Platform/system operations run outside clinic query filtering only through their explicit actor scope.

## Features

`FeatureDefinition` is global. `ClinicFeature` maps feature availability to one clinic.

Feature entitlement is separate from RBAC permission:

```text
valid session
  -> active clinic
  -> enabled feature (when applicable)
  -> required permission
  -> endpoint
```

V1 uses in-process memory caching only. Platform feature/status changes invalidate the relevant memory-cache entries. Redis is not part of the current runtime.

## Revocable sessions

Access JWTs contain a `session_id` linked to a persisted refresh-token row. Signature/lifetime validation is followed by server-side session validation.

```text
login -> S1
refresh S1 -> revoke S1, issue S2
logout S2 -> revoke S2
```

An access token tied to a revoked session is rejected immediately even when its JWT `exp` has not elapsed.

Refresh rotation is atomic at the database level so concurrent reuse of the same refresh token cannot create multiple valid replacement sessions.

## Audit

Audit records are append-only and capture scope, clinic (when applicable), actor snapshot, action/category, entity, metadata, network/request context, and timestamp.

Automatic EF auditing covers entity mutations. Explicit audit events cover authentication, refresh/logout, authorization denial, provisioning, tenant status changes, feature changes, file downloads, and similar non-CRUD events.

Platform audit visibility does not automatically expose clinic-user clinical activity.

## API route convention

Implemented routes follow:

```text
/api/{controller-name}/{endpoint-name}
```

Examples:

```text
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

GET  /api/audit-logs/search
GET  /api/audit-logs/get/{id}
GET  /api/platform-audit-logs/search
```

Swagger `OperationId` values remain stable API identifiers and are covered by contract tests.

## Automated security verification

CI applies all migrations to SQL Server and runs automated tests covering:

- Platform bootstrap and clinic provisioning.
- Clinic login/current-user flow.
- Multilingual permission catalog.
- Refresh rotation and replay/session revocation.
- Logout revocation.
- Clinic suspension/resume.
- Clinic Super User effective permissions.
- Self-disable behavior.
- Tenant query filtering and cross-clinic write rejection.
- Concurrency-safe business-code generation.

Manual endpoint testing is intentionally performed only after automated CI is green.
