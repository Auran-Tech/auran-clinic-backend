# Authentication & RBAC Foundation

## Purpose

Auran Clinic authentication and authorization are clinic-aware from the start so every business module can rely on one consistent identity, clinic, session, role, and permission model.

## Authentication model

- ASP.NET Core Identity owns credential persistence, password hashing, failed-login counters, and temporary security lockout.
- The domain `User` is the business account record and references the Identity user through `IdentityUserId`.
- Identity role stores are not used. Auran domain `Role`, `UserRole`, and `RolePermission` are the single source of truth for application RBAC.
- Login returns a short-lived JWT access token and a rotating refresh token.
- Refresh tokens are stored as SHA-256 hashes; raw refresh tokens are returned only to the client.
- Refresh rotation consumes one active session atomically and creates its replacement.
- Logout revokes only a session owned by the authenticated user and clinic.

## Business account state vs Identity lockout

These are deliberately separate concerns:

- `User.IsActive` is the business enable/disable state controlled by Auran Clinic authorization rules.
- ASP.NET Identity lockout is temporary attack protection after repeated failed login attempts.
- Disabling a business account revokes its active refresh sessions and makes existing access tokens fail active-state validation.
- Re-enabling a business account does not silently clear an independent Identity security lockout.
- Disabling a clinic makes existing clinic access tokens fail active-state validation for every clinic user.

Account-status endpoints follow the standard controller/action route convention:

- `PUT /api/users/status` requires `Users_Manage_Status`.
- `POST /api/users/disable-self` lets an authenticated account owner disable their own business account without the management permission.
- A normal manager cannot change another protected Super User account.
- A clinic Super User can change another clinic Super User because the backend issues the relevant effective permission claim.
- Cross-clinic target users remain invisible through the clinic query boundary and resolve as not found.

## JWT claims and active-session validation

Clinic access tokens contain `user_id`, `clinic_id`, `session_id`, `super_user`, role claims, and effective permission claims.

The `super_user` claim describes account type/context; it is not an authorization bypass. Permission authorization succeeds only when the JWT contains the required `permission` claim.

Every authenticated clinic request also validates current server-side state. The access token is rejected when any of these are no longer valid:

- the business user is inactive;
- the clinic is inactive;
- the referenced session is missing, revoked, or expired.

This provides immediate business-account and clinic shutdown semantics without waiting for the JWT expiry time.

## RBAC rules

- A user may have multiple roles through `UserRole`.
- Roles aggregate permissions through `RolePermission`.
- Normal users receive the distinct union of known permissions assigned through their roles.
- Super Users receive all known clinic permission keys from the backend permission catalog, even when they have no role assignments.
- Authorization handlers do not special-case `super_user=true`; they require the same matching permission claim for every account.
- Unknown or stale permission rows are excluded from effective authorization grants.

## Permission keys and localization

Authorization uses immutable backend keys. Keys use underscore notation, for example:

- `Patient_View`
- `Users_Manage_Status`
- `Attendance_Create_Shift`

The database entity keeps the stable key in `Permission.Code`; API responses expose it as `Key`.

Human-readable descriptions are separate localized data in `PermissionTranslation(PermissionId, LanguageCode, Description)`. English (`en`) and Arabic (`ar`) are seeded initially, and additional languages can be added without changing authorization keys or the schema.

`GET /api/permissions/list` returns the applicable permission catalog with stable keys and all stored descriptions.

## Permission policy usage

Application code should reference permission constants rather than literal strings:

```csharp
[Authorize(Policy = PermissionPolicy.Prefix + Permissions.Patients.View)]
```

`Permissions.Patients.View` resolves to the stable backend key `Patient_View`.

## Multi-clinic boundary

The authenticated token carries the current clinic identity, but client-supplied tenant identifiers are never trusted as the authorization boundary.

Persistence currently enforces clinic isolation through:

- global EF query filters for clinic-owned entities;
- write-time guards that reject cross-clinic inserts, updates, deletes, and `ClinicId` changes;
- server-side active user/clinic/session validation for JWT requests.

Clinic-owned child-to-parent relationships should additionally use SQL-level tenant invariants so cross-clinic foreign-key references are impossible even outside normal application code.

## Refresh token lifecycle

1. Successful login creates a cryptographically random refresh token and a persisted session record.
2. Only the SHA-256 token hash is stored.
3. Refresh validates the session plus active clinic/user state and Identity security state.
4. A conditional atomic update revokes exactly the consumed active token, preventing replay/concurrent reuse.
5. A replacement session is created and linked to the consumed session.
6. A new access token and raw refresh token are returned.

## Configuration and runtime hardening

`Jwt` settings contain Issuer, Audience, SigningKey, AccessTokenMinutes, and RefreshTokenDays. Startup validates the configuration, including a minimum signing-key length. Production secrets must be supplied through environment/secret configuration and never committed.

The API also provides login rate limiting, CORS configuration, global exception handling, forwarded-header support, and separate live/readiness health endpoints.

## Security notes

- Passwords are never stored in the domain user table.
- ASP.NET Core Identity owns password hashing and verification.
- Refresh tokens are never stored in plaintext.
- JWT signing keys must be secret-managed in production.
- Permission authorization uses backend-issued effective claims, not frontend logic or a Super User bypass.
- Authentication does not replace clinic-scoped persistence safeguards; tenant isolation is enforced independently from JWT contents.

## V1 foundation status

Implemented foundation includes login, JWT generation, refresh rotation/replay protection, logout/revocation, active-session validation, current-user/clinic context, domain RBAC, stable localized permission catalog, backend-issued Super User permissions, business account status, clinic shutdown behavior, dynamic permission policies, and SQL-backed integration coverage.

Future authentication capabilities such as MFA, password-reset/email delivery, invitation workflows, and external identity providers can be added on top of this foundation without replacing the current RBAC model.
