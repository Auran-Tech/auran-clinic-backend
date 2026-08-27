# Authentication & RBAC Foundation

## Purpose

This feature establishes the authentication and authorization foundation for Auran Clinic. It is intentionally clinic-aware from the beginning so future modules can rely on a consistent user, clinic and permission context.

## Authentication model

- ASP.NET Core Identity stores credentials and password hashes.
- The domain `User` remains the business user record and references the Identity user through `IdentityUserId`.
- Login returns a short-lived JWT access token and a rotating refresh token.
- Refresh tokens are stored as SHA-256 hashes; raw refresh tokens are returned only to the client.
- Refresh token rotation revokes the consumed token and creates a replacement.
- Logout revokes the supplied refresh token.

## JWT claims

Access tokens contain `user_id`, `clinic_id`, `super_user`, role claims and permission claims. This allows normal API authorization to avoid a database query on every request.

## RBAC rules

- A user may have multiple roles through `UserRole`.
- Roles aggregate permissions through `RolePermission`.
- System roles are platform-defined. Their identity and permissions are not intended to be renamed, deleted or edited through clinic administration.
- A Super User bypasses permission checks and is expected to see every page and capability inside the clinic.
- Normal users receive the union of permissions from all assigned roles.
- Permission policy names use `Permission:<PermissionCode>`, for example `Permission:Patients.View`.

## Multi-clinic boundary

The authenticated token carries `ClinicId`. Application services must always scope clinic-owned data using the authenticated clinic context. A client-supplied ClinicId must never be trusted as the authorization boundary.

## Refresh token lifecycle

1. Successful login creates a cryptographically random refresh token.
2. Only its SHA-256 hash is persisted.
3. Refresh validates that the token exists, belongs to an active user and has not expired/revoked.
4. The old token is revoked and replaced atomically.
5. A new access token and refresh token are returned.

## Permission usage

```csharp
[Authorize(Policy = PermissionPolicy.For(Permissions.Patients.View))]
```

Super users satisfy every permission requirement automatically.

## Configuration

`Jwt` settings contain Issuer, Audience, SigningKey, AccessTokenMinutes and RefreshTokenDays. Production signing keys must be provided through environment/secret configuration and must not be committed to source control.

## Security notes

- Passwords are never stored in the domain user table.
- ASP.NET Core Identity owns password hashing and verification.
- Refresh tokens are never stored in plaintext.
- JWT signing keys must be secret-managed in production.
- Authentication is not a replacement for clinic-scoped queries; tenant isolation must still be enforced in services/repositories.

## V1 scope

Included: login, JWT generation, refresh token rotation, logout/revocation, current-user context, multiple roles per user, permission claims, dynamic permission policies and Super User bypass.

Not included yet: MFA, password reset/email delivery, account invitation workflow, external identity providers, owner subscription portal, cross-clinic user switching and patient mobile authentication.
