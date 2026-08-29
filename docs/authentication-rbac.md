# Authentication & RBAC Foundation

## Purpose

AURAN Clinic has two intentionally separate security scopes: the AURAN platform and an individual clinic tenant. A Platform Admin is not a clinic Super User, and a clinic Super User must never gain platform or cross-tenant access.

## Identity model

ASP.NET Core Identity is the shared credential store. `ApplicationIdentityUser.AccountType` is persisted as a string and identifies whether an Identity account is `Platform` or `Clinic`.

Business identities remain separate:

```text
Identity account (AccountType=Platform) -> PlatformUser
Identity account (AccountType=Clinic)   -> User -> Clinic
```

A fake AURAN clinic is never used to represent platform administrators.

## Platform authentication

Platform authentication uses `/api/platform/auth/*`. Platform JWTs contain:

- `actor_type=Platform`
- `platform_user_id`
- `session_id`
- `platform_role`
- `platform_permission`
- identity/display claims

A platform token does not contain `clinic_id` and cannot satisfy clinic-actor authorization policies.

The initial Platform Admin is created only through the deployment-time Platform Bootstrap process. There is no public platform registration endpoint. Bootstrap credentials must come from environment/secret configuration.

## Clinic authentication

Clinic authentication remains under `/api/auth/*`. Clinic JWTs contain:

- `actor_type=Clinic`
- `clinic_user_id`
- `clinic_id`
- `clinic_super_user`
- `session_id`
- `clinic_role`
- `clinic_permission`
- identity/display claims

Login and refresh are rejected when the clinic is inactive. Protected clinic requests also pass through the clinic actor guard, so suspension applies to already-issued access tokens after the clinic-status cache is invalidated.

## Refresh tokens and revocable sessions

Platform and clinic refresh tokens use separate persistence models. Both use cryptographically random raw tokens, persist only SHA-256 hashes, revoke consumed tokens during rotation, and create replacement tokens. Raw refresh tokens are returned only to the client.

Each access token is bound to the persisted refresh-token row for the same authentication session through the `session_id` claim. JWT signature, issuer, audience and lifetime validation still run normally, then the API verifies that the referenced session is still active.

This gives immediate session termination without adding a separate JWT blacklist:

```text
Login
  -> AccessToken(session_id=S1) + RefreshToken(row Id=S1)

Refresh S1
  -> revoke S1
  -> AccessToken(session_id=S2) + RefreshToken(row Id=S2)
  -> old access token S1 is rejected immediately

Logout S2
  -> revoke S2
  -> access token S2 is rejected immediately
```

A revoked or expired session causes protected requests to fail authentication even if the JWT's cryptographic signature and `exp` are otherwise valid. This applies consistently to both platform and clinic authentication.

## Authorization scopes

Permission policies are explicit about scope:

```csharp
[Authorize(Policy = PermissionPolicy.ForPlatform(Permissions.Platform.Clinics.Create))]
[Authorize(Policy = PermissionPolicy.ForClinic(Permissions.Clinic.Patients.View))]
```

Platform permissions use the `PlatformPermission:` policy prefix. Clinic permissions use `ClinicPermission:`.

The dynamic policy provider also requires the matching actor requirement, so a clinic token cannot satisfy a platform permission even if a claim name is manipulated or a similarly named permission exists.

## Platform RBAC

Platform RBAC is separate from clinic RBAC:

```text
PlatformUser -> PlatformUserRole -> PlatformRole -> PlatformRolePermission -> Permission(scope=Platform)
```

The initial protected role is `PLATFORM_ADMIN`. The model can later support Platform Support, Sales or Operations without changing clinic RBAC.

Current platform permissions cover clinic viewing/provisioning/updating/status, clinic feature management, platform audit and future platform-user management.

## Clinic RBAC

Clinic RBAC remains tenant scoped:

```text
User -> UserRole -> Role -> RolePermission -> Permission(scope=Clinic)
```

Protected clinic roles are Admin, Receptionist, Doctor and Nurse. A clinic user may have multiple roles and receives the union of their role permissions.

`IsClinicSuperUser` bypasses clinic permission checks only after the request is proven to be an authenticated, active clinic actor. It never bypasses platform authorization and never grants access to another clinic.

## Tenant boundary

Clinic-owned data is scoped by the authenticated `clinic_id`. Clinic-facing endpoints do not accept an arbitrary clinic id as their authorization boundary. For example, clinic settings are exposed as `/api/clinic/settings`, while cross-tenant clinic lifecycle operations are exposed only under `/api/platform/clinics`.

Platform administration does not imply access to patient or clinical records. Future support access must be an explicit, time-bound and fully audited mechanism rather than an implicit Platform Admin capability.

## Features versus permissions

Feature entitlement and user permission are separate checks:

```text
Clinic active -> Feature enabled -> User permission -> Endpoint
```

Features are controlled by AURAN platform administration. Clinic users can read their feature availability but cannot change it. Feature state is read through the centralized clinic-access service and cached through the configured distributed cache.

## Security audit

Authentication success/failure, token rotation, logout and authorization denials are audited. Platform and clinic actor identity is captured as an immutable snapshot in audit records. Passwords, tokens, signing keys, connection strings and other secrets are redacted and must never be persisted in audit metadata.

## Configuration

`Jwt` contains Issuer, Audience, SigningKey, AccessTokenMinutes and RefreshTokenDays. The repository does not contain a production signing key. Development has a development-only key; production must supply `Jwt__SigningKey` from secret/environment configuration.

`PlatformBootstrap` is disabled by default. When deliberately enabled for first deployment, `FullName`, `Email` and `Password` must be supplied securely. After a Platform user exists, bootstrap remains idempotent and does not create duplicate administrators.

## Deferred security features

MFA, password reset/email delivery, invitation workflows, external identity providers, platform support impersonation/break-glass access, subscriptions/billing and cross-clinic account switching are deferred until explicitly designed.
