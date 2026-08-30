# Authentication & RBAC Foundation

## Authentication

- ASP.NET Core Identity owns credentials and password hashing.
- Domain `User` stores clinic/business identity and references the Identity user by `IdentityUserId`.
- Login returns a JWT access token plus a rotating refresh token.
- Refresh tokens are cryptographically random; only SHA-256 hashes are persisted.
- Refresh rotation is optimistic-concurrency protected.
- Logout can revoke only a refresh token belonging to the authenticated user and clinic.

## Current Context

`ICurrentUserContext` is the only request-context abstraction:

```text
IsAuthenticated
UserId
ClinicId
IsSuperUser
```

JWT access tokens contain `user_id`, `clinic_id`, `super_user`, role claims and effective permission claims.

## Account and Clinic State

`User.IsActive` is the explicit individual account state. `Clinic.IsActive` is the clinic-wide state.

Login, refresh and JWT validation all require both states to be active. This means disabling a clinic rejects all accounts immediately, including requests using access tokens issued before the clinic was disabled.

A user can disable their own account. Administrative account status changes require `Users_Manage_Status` or the protected Super User bypass. A normal manager cannot disable another Super User. Disabling a user revokes all active refresh tokens.

## RBAC

Identity roles are not used for business authorization. Auran owns:

```text
Role
Permission
UserRole
RolePermission
```

Normal users receive the union of permission keys assigned by all their roles.

A Super User:

- remains limited to their own clinic;
- satisfies every permission requirement in the backend;
- receives the complete permission key catalog from login and `/api/auth/me`.

The frontend therefore consumes backend-calculated effective permissions and does not create a separate Super User permission model.

## Permission Keys and Localization

Stable permission keys use underscore identifiers, for example:

```text
Patient_View
Patient_Create
Queue_Move
Users_Manage_Status
```

`Permission` stores the stable key and grouping metadata. `PermissionTranslation` stores localized human descriptions by language code. English and Arabic are initially seeded. Additional languages are inserted as data without changing the schema or permission key.

Permission policies continue to use the `Permission:` policy prefix internally:

```csharp
[Authorize(Policy = PermissionPolicy.Prefix + Permissions.PatientView)]
```

## Multi-Clinic Boundary

Authentication does not replace tenant isolation. Clinic-owned EF entities are automatically query-filtered by the current clinic, and the DbContext rejects authenticated cross-clinic writes.

`IgnoreQueryFilters()` is reserved for explicit infrastructure cases such as login/refresh where no authenticated clinic context exists yet and ownership/status are validated directly.

## API

```text
POST /api/auth/login
GET  /api/auth/me
POST /api/auth/refresh
POST /api/auth/logout
GET  /api/permissions/list
PUT  /api/users/status
POST /api/users/disable-self
```

The login endpoint is rate-limited and request DTOs use FluentValidation.
