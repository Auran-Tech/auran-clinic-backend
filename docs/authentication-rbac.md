# Authentication & RBAC Foundation

## Purpose

Auran Clinic has two intentionally separate security scopes: the Auran platform and an individual clinic tenant. A Platform Admin is not a Clinic Super User, and a Clinic Super User must never gain platform or cross-tenant access.

## Identity model

ASP.NET Core Identity is the shared credential store. `ApplicationIdentityUser.AccountType` identifies whether an Identity account is `Platform` or `Clinic`.

Business identities remain separate:

```text
Identity account (AccountType=Platform) -> PlatformUser
Identity account (AccountType=Clinic)   -> User -> Clinic
```

ASP.NET Identity roles are not used for Auran business authorization. The EF Identity context persists users, claims, logins and tokens; Auran owns its Platform and Clinic RBAC models.

## Platform authentication

Platform authentication uses:

```text
POST /api/platform-auth/login
POST /api/platform-auth/refresh
POST /api/platform-auth/logout
```

Platform JWTs contain `actor_type=Platform`, `platform_user_id`, `session_id`, platform roles/permission keys and normal identity/display claims. They do not contain a ClinicId and cannot satisfy Clinic actor policies.

The initial Platform Admin is created only through deployment-time Platform Bootstrap. There is no public platform-registration endpoint.

## Clinic authentication

Clinic authentication uses:

```text
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
GET  /api/auth/me
```

Clinic JWTs contain `actor_type=Clinic`, `clinic_user_id`, `clinic_id`, `clinic_super_user`, `session_id`, role claims and effective permission-key claims.

Login uses ASP.NET Identity credential checks with failed-password lockout enabled. Identity lockout is a temporary security control and is separate from Auran's business account state.

`User.IsActive` is the individual business account state. `Clinic.IsActive` is the clinic-wide state. Login, refresh and authenticated JWT session validation require both to remain active. Suspending a clinic therefore blocks all clinic accounts, including already-issued access tokens.

## Refresh tokens and revocable sessions

Platform and Clinic refresh tokens use separate persistence models. Both use cryptographically random raw tokens, persist only SHA-256 hashes, revoke consumed tokens atomically during rotation and create a replacement session. Raw refresh tokens are returned only to the client.

Each access token is bound to a persisted session through `session_id`:

```text
Login
  -> AccessToken(session_id=S1) + RefreshToken(row Id=S1)

Refresh S1
  -> atomically revoke S1
  -> AccessToken(session_id=S2) + RefreshToken(row Id=S2)
  -> old access token S1 is rejected immediately

Logout S2
  -> revoke S2 owned by the authenticated actor
  -> access token S2 is rejected immediately
```

Logout never revokes a refresh token owned by another Clinic user, Clinic, or Platform user.

## Permission model

Permission authorization is backend owned. A stable key is never translated and never chosen by the frontend. Examples:

```text
Patient_View
Patient_Create
Queue_Move
Users_Manage_Status
Platform_Clinics_Create
```

Persistence is split deliberately:

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

The `(PermissionId, LanguageCode)` pair is unique. English (`en`) and Arabic (`ar`) descriptions are provisioned initially. German, French or any later client language is added as data without changing the Permission table or authorization key.

The permission catalog endpoint is:

```text
GET /api/permissions/list
```

It returns the stable key, group, scope and all stored descriptions visible to the authenticated actor scope.

## Authorization scopes

Permission policies are explicit about security scope. Platform policies use `PlatformPermission:` and Clinic policies use `ClinicPermission:`. The dynamic policy provider also requires the matching actor type, so a similarly named claim from the wrong actor scope cannot satisfy a policy.

## Platform RBAC

```text
PlatformUser -> PlatformUserRole -> PlatformRole -> PlatformRolePermission -> Permission(scope=Platform)
```

The initial protected role is `PLATFORM_ADMIN`. Platform login returns the backend-calculated union of Platform permission keys assigned through its roles.

## Clinic RBAC

```text
User -> UserRole -> Role -> RolePermission -> Permission(scope=Clinic)
```

Protected Clinic roles are Admin, Receptionist, Doctor and Nurse. A user may have multiple roles and receives the union of their role permissions.

A Clinic Super User is a protected backend concept:

- it remains restricted to its own ClinicId;
- it satisfies every Clinic permission policy;
- login, refresh and `/api/auth/me` return the complete Clinic permission catalog from the backend;
- the frontend must not invent or bypass permissions for a Super User.

## Account-state administration

Business account status is changed through backend authorization:

```text
PUT  /api/users/status
POST /api/users/disable-self
```

A user may disable their own account. Changing another clinic user's state requires `Users_Manage_Status` or the Clinic Super User bypass. A normal manager cannot disable another protected Super User. Disabling a user revokes that user's active refresh sessions.

## Tenant boundary

All Clinic-owned EF Core entities carry `ClinicId`. Authenticated Clinic queries are globally tenant filtered, and `SaveChanges` rejects cross-clinic writes. Clinic-facing endpoints resolve their tenant from authenticated context rather than caller-supplied ClinicId values.

Platform administration does not imply access to patient or clinical records. Any future support-access mechanism must be explicit, time-bound and audited.

## Features versus permissions

Feature entitlement and user permission remain separate checks:

```text
Clinic active -> Feature enabled -> User permission -> Endpoint
```

V1 cache access uses memory-backed `IDistributedCache`. Redis is not an active V1 dependency.

## Security audit

Authentication success/failure, rotation, logout and authorization denials are audited. Actor identity is captured as a snapshot. Passwords, JWTs, refresh tokens, signing keys, connection strings and other secrets must never be persisted in audit metadata.

## Configuration

`Jwt` contains Issuer, Audience, SigningKey, AccessTokenMinutes and RefreshTokenDays. The repository contains no usable signing secret. `Jwt__SigningKey` must be supplied by environment/secret configuration and startup fails when the signing key is missing or too short.

`PlatformBootstrap` is disabled by default. When deliberately enabled for first deployment, its administrator credentials must be supplied securely.

## Deferred security features

MFA, password reset/email delivery, invitation workflows, external identity providers, platform support impersonation/break-glass access, subscriptions/billing and cross-clinic account switching remain deferred until explicitly designed.
