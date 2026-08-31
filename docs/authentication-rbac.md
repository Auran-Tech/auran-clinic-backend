# Authentication & RBAC Foundation

## Purpose

AURAN Clinic has two intentionally separate security scopes: the AURAN Platform and an individual Clinic tenant.

A Platform Admin is not a Clinic Super User. A Clinic Super User must never gain platform or cross-tenant access.

## Identity model

ASP.NET Core Identity is the shared credential store.

`ApplicationIdentityUser.AccountType` identifies whether a credential belongs to:

```text
Platform
Clinic
```

Business identities remain separate:

```text
Identity(AccountType=Platform) -> PlatformUser
Identity(AccountType=Clinic)   -> User -> Clinic
```

The persistence context uses the user-only Identity model. AURAN domain roles/permissions are the application RBAC system; unused `AspNetRoles`, `AspNetUserRoles`, and `AspNetRoleClaims` are not part of the current model.

## Current actor

`ICurrentActor` is the single request actor abstraction. It exposes:

```text
IsAuthenticated
ActorType
IdentityUserId
PlatformUserId
ClinicUserId
ClinicId
IsClinicSuperUser
DisplayName
Email
```

Clinic context is derived from validated JWT claims and cannot be switched by a client-supplied `ClinicId`.

## Platform authentication

Routes:

```text
POST /api/platform-auth/login
POST /api/platform-auth/refresh
POST /api/platform-auth/logout
```

Platform JWTs conceptually contain:

```text
actor_type=Platform
platform_user_id
session_id
platform_role
platform_permission
identity/display claims
```

A Platform token does not contain `clinic_id` and cannot satisfy Clinic actor policies.

The first Platform Admin is created only through secure deployment-time bootstrap configuration. There is no public Platform Admin registration endpoint.

## Clinic authentication

Routes:

```text
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
GET  /api/auth/me
```

Clinic JWTs conceptually contain:

```text
actor_type=Clinic
clinic_user_id
clinic_id
clinic_super_user
session_id
clinic_role
clinic_permission
identity/display claims
```

Clinic login and refresh reject:

- inactive `User`,
- inactive `Clinic`,
- invalid credentials,
- expired/revoked refresh sessions,
- Identity lockout where applicable.

Protected requests also validate the current persisted session, user state, and clinic state so previously issued access tokens do not continue working after revocation/suspension.

## Identity lockout versus account disable

These are separate controls.

### Identity lockout

A temporary authentication-security mechanism triggered by repeated invalid password attempts.

### Business account state

`User.IsActive` is controlled by application rules. It determines whether the clinic account is enabled.

Administrative status changes use the `Users_Manage_Status` permission, while the current user can explicitly disable their own account.

Clinic suspension is represented by `Clinic.IsActive=false` and blocks all accounts belonging to that clinic.

## Revocable sessions

Clinic and Platform refresh-token records represent server-side authentication sessions.

Each access token contains `session_id` equal to its persisted session row id.

```text
Login
  -> AccessToken1(session S1)
  -> RefreshToken1(session S1)

Refresh S1
  -> atomically revoke S1
  -> create S2
  -> old access token S1 rejected immediately

Logout S2
  -> revoke S2
  -> access token S2 rejected immediately
```

Raw refresh tokens are cryptographically random and are never persisted. The database stores SHA-256 hashes.

Refresh rotation uses an atomic database update/transaction so concurrent replay of the same refresh token cannot create multiple valid replacement sessions.

Clinic logout also verifies that the supplied refresh session belongs to the authenticated clinic user.

## Permission model

Permission keys use stable underscore identifiers:

```text
Patient_View
Patient_Create
Users_Manage_Status
Queue_Move
Visit_Edit
Settings_Manage
Platform_Clinics_Create
```

Persistence is language-independent:

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

English and Arabic are seeded. Additional languages are data changes, not schema changes.

The permission catalog endpoint is:

```text
GET /api/permissions/list
```

It returns the actor-appropriate scope with all stored descriptions.

## Clinic RBAC

Clinic RBAC graph:

```text
User
  -> UserRole
  -> Role
  -> RolePermission
  -> Permission(scope=Clinic)
```

Protected initial roles:

```text
Admin
Receptionist
Doctor
Nurse
```

A user may have multiple roles. Effective normal-user permissions are the union of role permissions.

### Clinic Super User

`User.IsClinicSuperUser` is protected user state, not a role.

For a Clinic Super User, the backend returns and authorizes against every clinic-scoped permission in the catalog. The frontend must not create its own Super User bypass logic.

Super User bypass applies only after the request is proven to be a valid active Clinic actor for that tenant.

## Platform RBAC

Platform RBAC graph:

```text
PlatformUser
  -> PlatformUserRole
  -> PlatformRole
  -> PlatformRolePermission
  -> Permission(scope=Platform)
```

The protected initial platform role is:

```text
PLATFORM_ADMIN
```

Platform permissions are separate from Clinic permissions and cannot cross-satisfy policies.

## Tenant boundary

Clinic-owned entities inherit `ClinicEntity` and carry `ClinicId`.

Tenant isolation is enforced centrally:

- global EF Core query filters restrict reads,
- SaveChanges guards restrict writes,
- clinic endpoints derive tenancy from the authenticated actor,
- platform operations use explicit Platform actor policies.

## Permission policies

Dynamic policies are scope-specific:

```text
ClinicPermission:<key>
PlatformPermission:<key>
```

A policy succeeds only for the matching actor scope and permission conditions.

## Cache behavior

V1 uses memory caching only for short-lived access/status/feature data.

Redis is not part of the current runtime.

Status/feature changes invalidate relevant memory-cache entries so authorization state changes take effect without waiting for cache expiration.

## API security foundation

The API also includes:

- JWT issuer/audience/signature/lifetime validation,
- Bearer-header normalization for malformed duplicated/quoted Bearer input,
- login rate limiting,
- CORS allow-list configuration,
- global exception handling,
- structured authorization failures,
- secret redaction in audit,
- `/health/live`,
- `/health/ready` with DB readiness.

Production signing keys and bootstrap credentials are supplied by deployment secrets/environment configuration and are not committed.

## Automated verification

CI includes tests for:

- authentication contracts,
- actor/session validation,
- refresh/logout revocation,
- disabled user/clinic rejection,
- full Platform bootstrap → clinic provisioning → Clinic auth lifecycle,
- multilingual permission responses,
- Super User full effective permissions,
- tenant isolation,
- SQL Server migration/model consistency.

Manual endpoint verification remains the final test stage after automated CI is green.
