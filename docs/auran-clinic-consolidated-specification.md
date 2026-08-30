# AURAN Clinic — Consolidated Architecture, Product Decisions and Implementation Notes

_Last updated: 2026-08-30_

This document consolidates the important product, architecture, security, data-model, API, testing and implementation decisions discussed during the current AURAN Clinic backend work. It is intended to act as a durable project reference so decisions do not remain only in chat history.

> Status note: PR #5 (`feature/clinic-management-audit-foundation`) is still under active review and must not be merged until explicitly approved.

---

## 1. Product Direction

AURAN Clinic is being built as a multi-tenant clinic-management product using one frontend codebase and one backend codebase. Different clinics are tenants/customers and differences between customers should be driven by configuration and data, not by customer-specific source-code copies.

The V1 goal is to establish the clinic/platform foundation first, then build patient, queue, visit and clinical modules on top of it.

### Initial clinic roles

The protected system roles are:

- Admin
- Receptionist
- Doctor
- Nurse

System roles cannot be renamed or deleted and their built-in role identity is protected. Users can have multiple roles and effective permissions are the union of their role permissions.

### Clinic Super User

A Clinic Super User is different from a Platform Admin.

A Clinic Super User:

- belongs to exactly one clinic,
- bypasses clinic RBAC only inside that clinic,
- sees all clinic pages/actions,
- cannot be restricted by ordinary clinic administrators,
- cannot access or manage other clinics,
- cannot access platform administration merely because they are a clinic super user.

### Platform Admin

Platform users are AURAN-side operators, not clinic users. Platform access is intentionally separated from clinic access.

A Platform Admin can:

- create/provision clinics,
- list clinics,
- update clinic metadata,
- activate/suspend clinics,
- manage clinic feature entitlements,
- manage future platform users/roles,
- read platform-operational audit data within the defined platform scope.

A Platform Admin does **not** automatically gain unrestricted patient/clinical-data access. Future support access to clinical data must use an explicit, time-limited, heavily audited support/break-glass mechanism rather than implicit platform privilege.

---

## 2. Core Technology and Solution Architecture

The backend stack is:

- .NET 10
- ASP.NET Core Web API
- SQL Server
- Entity Framework Core
- ASP.NET Core Identity
- JWT authentication
- FluentValidation
- Serilog
- Swagger/OpenAPI
- Memory or Redis caching
- Modular Monolith architecture

There is no API version prefix at this stage; routes use `/api/...`.

### Projects

```text
src/Auran.Clinic.Api
src/Auran.Clinic.Application
src/Auran.Clinic.Domain
src/Auran.Clinic.Infrastructure

tests/Auran.Clinic.UnitTests
tests/Auran.Clinic.IntegrationTests
```

There is intentionally no separate Shared project/layer.

### Controller/service rule

Controllers remain thin. Business behavior belongs in Application/Infrastructure services.

### Common response models

```csharp
public class BaseResponse
{
    public string? Message { get; set; }
    public bool Status { get; set; }
    public string? Error { get; set; }
}

public class BaseResponse<T> : BaseResponse where T : class
{
    public T? Data { get; set; }
}
```

Pagination uses:

```csharp
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new List<T>();
    public required PaginationInfo Setting { get; set; }
}
```

with `PaginationInfo` containing `TotalCount`, `RowCount`, `CurrentPage` and calculated `TotalPage`.

---

## 3. Multi-Tenant Model

The system uses one application for many clinics.

### Rules

- All clinic-owned business entities are scoped by `ClinicId`.
- Clinic endpoints derive the tenant from the authenticated token; the client must not be allowed to switch tenant using a route/query `ClinicId`.
- Platform and clinic authentication/authorization scopes are intentionally separate.
- Tenant security must be correct from the beginning even though future SaaS/account abstractions are not being overbuilt yet.

### Domain user separation

Clinic user:

```text
User : ClinicEntity
```

Platform user:

```text
PlatformUser : BaseEntity
```

ASP.NET Identity remains the global credential store.

`ApplicationIdentityUser.AccountType` identifies whether an Identity credential belongs to a `Clinic` or `Platform` account.

There is no fake AURAN clinic used for platform administrators.

---

## 4. Authentication and Authorization

### Actor types

The authenticated actor is one of:

- Clinic
- Platform

System audit operations may additionally use `System` as an audit actor type.

### Clinic JWT claims

Conceptually:

```text
actor_type = Clinic
clinic_user_id = ...
clinic_id = ...
clinic_super_user = true/false
clinic_role = ...
clinic_permission = ...
session_id = ...
```

### Platform JWT claims

Conceptually:

```text
actor_type = Platform
platform_user_id = ...
platform_role = PLATFORM_ADMIN
platform_permission = ...
session_id = ...
```

A Platform token does not contain `clinic_id`.

The old generic `super_user=true` cross-scope concept is not allowed.

### Current actor abstraction

```csharp
ICurrentActor
{
    bool IsAuthenticated;
    ActorType ActorType;
    string? IdentityUserId;
    Guid? PlatformUserId;
    Guid? ClinicUserId;
    Guid? ClinicId;
    bool IsClinicSuperUser;
    string? DisplayName;
    string? Email;
}
```

### Scoped permission policies

Permissions are separated by scope.

Clinic permission succeeds only when:

- actor is Clinic,
- clinic is active,
- and the actor either has the required clinic permission or is the clinic's super user.

Platform permission succeeds only when:

- actor is Platform,
- and actor has the required platform permission.

No cross-scope bypass is permitted.

### Access-session revocation

JWTs are not treated as irrevocable until expiry.

Every issued access token is tied to a server-side session using `session_id`. Refresh-token records represent the authenticated session state.

Expected lifecycle:

```text
Login
  -> AccessToken1 + RefreshToken1 + Session1

Refresh RefreshToken1
  -> Session1 revoked
  -> AccessToken1 becomes invalid immediately
  -> RefreshToken1 becomes invalid
  -> AccessToken2 + RefreshToken2 + Session2

Logout using current session
  -> current session revoked
  -> current access token stops working immediately
  -> current refresh token stops working immediately
```

This behavior applies to both Platform and Clinic accounts.

### Bearer-token normalization

Manual testing exposed malformed Authorization headers such as duplicated `Bearer` prefixes or quoted values. The Bearer pipeline normalizes the header before JWT validation while still retaining full issuer/audience/signature/lifetime validation.

Swagger's Authorize dialog should receive the raw JWT only; Swagger adds the `Bearer` prefix.

---

## 5. Platform Bootstrap

There is no public Platform Admin registration endpoint.

The first platform account is created using deployment/bootstrap configuration.

Relevant environment/config keys:

```text
PlatformBootstrap__Enabled
PlatformBootstrap__Email
PlatformBootstrap__Password
PlatformBootstrap__FullName
PlatformBootstrap__Phone
```

Bootstrap is disabled by default. No real bootstrap credentials must be committed.

Bootstrap ensures:

- permission catalog,
- feature catalog,
- protected `PLATFORM_ADMIN` role,
- platform-role permission mappings,
- first platform Identity user,
- matching `PlatformUser`,
- role assignment,
- audit history.

If a platform administrator already exists, bootstrap must not create a duplicate.

---

## 6. Clinic Provisioning

Creating a clinic is a provisioning workflow, not a single insert.

```text
Platform Admin
  -> POST /api/platform/clinics
  -> Generate clinic business code
  -> Create Clinic
  -> Create default ClinicSettings
  -> Create ClinicFeature defaults
  -> Create protected clinic roles
       Admin
       Receptionist
       Doctor
       Nurse
  -> Assign default role permissions
  -> Create initial Admin Identity account
  -> Create clinic domain User
  -> Assign Admin role
  -> Write audit events
  -> COMMIT
```

The initial clinic Admin is a normal Admin-role user, not automatically a Clinic Super User.

Provisioning should be atomic: a failed provisioning step must not leave a partially created clinic.

---

## 7. Server-Generated Business Codes

Business codes are generated by the backend. The frontend must never generate or submit the final business code.

The user/operator controls only the prefix.

### Format

```text
PREFIX-YEAR-SEQUENCE
```

Examples:

```text
CDC-2026-1
CDC-2026-2
PT-2026-1
PT-2026-2
```

### CodeCounter concept

```text
CodeCounters
------------
Id
Scope
ClinicId nullable
CodeType
Prefix
Year
LastNumber
CreatedDate
UpdatedDate
CreateByUserId
UpdatedByUserId
```

### Scope

`CodeScope` is an enum:

```text
Platform
Clinic
```

### Code type

`CodeType` is an enum for business-generated identifiers, beginning with:

```text
Clinic
Patient
```

System identifiers such as role codes, permission codes and feature codes are **not** generated business codes and remain stable constants.

### Platform-scope example

Clinic creation happens before a tenant `ClinicId` exists:

```text
Scope      = Platform
ClinicId   = NULL
CodeType   = Clinic
Prefix     = CDC
Year       = 2026
LastNumber = 7
```

Next clinic code:

```text
CDC-2026-8
```

### Clinic-scope example

```text
Scope      = Clinic
ClinicId   = <clinic id>
CodeType   = Patient
Prefix     = PT
Year       = 2026
LastNumber = 125
```

Next patient number:

```text
PT-2026-126
```

### Concurrency requirement

Counter increments must be atomic at the database level. Never implement the counter as a plain `SELECT LastNumber`, increment in memory, then `UPDATE`, because concurrent requests could generate duplicates.

### Clinic creation contract

The create-clinic request accepts:

```text
CodePrefix
```

not final `Code`.

The generated `Clinic.Code` is immutable after creation.

---

## 8. Static Reference Data Strategy

Stable dropdown/reference values should not require database tables or database reads in V1.

### Principle

```text
Small/stable/system-owned values
  -> enum or static in-memory catalog

Stable UI/reference options
  -> static application catalog

Standard runtime data
  -> runtime/system catalog

Truly business-managed/dynamic data
  -> database table
```

A dropdown in the frontend does not automatically imply a database table.

### Static catalogs

Current catalog categories include:

- Fonts
- Countries
- Cities
- Locales
- Date formats
- Time formats

These are served directly from application memory.

### Time zones

Time zones are not modeled as a giant enum or DB table.

The backend uses runtime time-zone information and normalizes identifiers to IANA form when possible.

Preferred IDs include:

```text
Africa/Cairo
Asia/Riyadh
Asia/Dubai
Europe/London
America/New_York
```

### Country and city values

Country and city use stable catalog codes rather than DB primary keys in V1.

Example:

```text
CountryCode = EG
CityCode    = CAI
```

### Font values

Fonts are provided as a controlled catalog such as:

```text
Inter
Roboto
Arial
Open Sans
Cairo
Tajawal
```

### Reference endpoints

```http
GET /api/reference/fonts
GET /api/reference/countries
GET /api/reference/countries/{countryCode}/cities
GET /api/reference/locales
GET /api/reference/date-formats
GET /api/reference/time-formats
GET /api/reference/time-zones
```

These endpoints are anonymous reference/catalog operations and do not require reference-table DB queries.

---

## 9. Clinic Branding and Settings

Clinic configuration includes or is expected to include:

- logo,
- primary color,
- secondary color,
- font family,
- welcome title,
- welcome message,
- welcome button text,
- time zone,
- country code,
- city code,
- patient number prefix,
- phone,
- email,
- address,
- website,
- locale,
- date format,
- time format,
- documentation reminder hours,
- prescription header,
- prescription footer.

A successful login should show an in-app Welcome page once per login/session, with the normal sidebar/navbar visible. The Welcome page is not a sidebar destination. Clinics can configure the welcome title/message/button.

---

## 10. File Storage and Upload Architecture

Files are handled through one central file registry and a storage-provider abstraction.

Business entities should store stable `FileId` references instead of permanent external URLs wherever possible.

### Existing central file record concept

`FileRecord` is the permanent metadata registry. It includes metadata such as:

```text
Id
ClinicId
OriginalName
StoredName
ContentType
Size
StorageProvider
StorageKey
UploadedAtUtc
UploadedByUserId
```

The goal is to let the physical storage provider change without changing business references.

### Why business entities store FileId

A URL can change when moving from:

```text
Local server
  -> S3
  -> CDN/CloudFront
```

A permanent `FileId` does not need to change.

The API can compute/return the appropriate URL at response time.

### Upload-session flow

The frontend does not submit file binary as part of a business JSON request.

Expected flow:

```text
Frontend
  -> request temporary upload session
  -> backend creates short-lived scoped session
  -> backend returns UploadUrl + expiration + upload-session data
  -> frontend uploads binary to UploadUrl
  -> frontend completes the upload session
  -> backend verifies the uploaded object
  -> backend creates/activates permanent FileRecord
  -> backend returns FileId + URL + metadata
  -> business request stores FileId
```

### Local storage

For local storage, the returned upload URL points to the AURAN API, and the API writes the file to a tenant-scoped local storage path.

Conceptually:

```text
/storage/{clinicId}/{year}/{month}/{generatedStoredName}
```

### AWS/S3

The same API contract can later return an S3 presigned URL. The frontend then uploads directly to S3.

The FE workflow should remain the same regardless of Local vs S3.

### Temporary upload-session security

An upload session should track enough information to prevent arbitrary/unverified file registration, including concepts such as:

```text
ClinicId
UserId
OriginalFileName
ExpectedContentType
ExpectedSize or MaxSize
ExpiresAtUtc
StorageProvider
StorageKey
Status
```

Temporary upload authorization must:

- expire,
- be tenant scoped,
- be user/session scoped as appropriate,
- be one-use after successful upload,
- avoid storing raw secret tokens in the DB where possible,
- verify expected metadata before final completion.

### Business payload example

Preferred:

```json
{
  "fullName": "Ahmed Ali",
  "profileImageFileId": "..."
}
```

Avoid embedding Base64 file data into business JSON.

Avoid trusting arbitrary externally supplied URLs as proof of an uploaded/owned file.

### File use cases

The same foundation should support:

- clinic logo,
- patient photo,
- patient attachments,
- prescriptions,
- lab results,
- radiology images,
- clinical-order attachments,
- general documents.

---

## 11. Audit Architecture

Audit is centralized and append-only.

### Automatic CRUD audit

An EF Core `SaveChanges` interceptor automatically captures create/update/delete operations for auditable entities.

### Explicit audit events

Some events are not CRUD transitions and are written explicitly, including:

- login,
- logout,
- failed login,
- refresh,
- permission denied,
- report export,
- file download,
- sensitive reads where required,
- clinic provisioning,
- activation/suspension,
- feature changes.

### AuditLog shape

Conceptually:

```text
AuditScope            Platform | Clinic
ClinicId              nullable
ActorType              System | Clinic | Platform
ActorId                nullable
ActorIdentityUserId
ActorDisplayName
ActorEmail
Action
Category
EntityType
EntityId
Description
OccurredAtUtc
MetadataJson
IpAddress
UserAgent
CorrelationId
CreatedDate
CreateByUserId
```

Audit actor information is stored as a snapshot rather than depending exclusively on current user FKs.

### Security and redaction

Audit metadata must redact sensitive values such as:

- password,
- token,
- secret,
- signing key,
- connection string,
- API key,
- credential.

### Audit visibility

Clinic users:

- can see only audit entries for their own clinic,
- cannot expand visibility by providing another `ClinicId`.

Platform users:

- can see platform-scope operations,
- can see clinic-scope actions performed by platform actors,
- do not automatically receive broad access to clinic-user clinical audit metadata.

Audit records do not expose ordinary update/delete APIs.

---

## 12. Feature Entitlements

Feature entitlements are different from user permissions.

- Feature = capability enabled for the clinic/tenant.
- Permission = authorization for a user within enabled capabilities.

### Global feature definitions

Initial feature codes:

```text
Patients                 default true
DynamicPatientProfile    default true
Queue                    default true
Visits                   default true
ClinicalOrders           default true
FollowUps                default true
Reports                  default true
AdvancedReports          default false
AI                       default false
```

### Tenant mapping

```text
ClinicFeature
  ClinicId
  FeatureDefinitionId
  IsEnabled
  ConfigurationJson
```

### Cache keys

Conceptually:

```text
clinic:{id}:active
clinic:{id}:feature:{code}
```

Feature state is not embedded permanently into JWT claims so disabling a feature does not require waiting for access-token refresh.

---

## 13. Patient Profile and Clinical-Data Decisions

### Dynamic patient profile

The selected model is Dynamic Rows, not a hybrid fixed+duplicated structure.

Core entities:

```text
PatientProfileField
PatientProfileFieldOption
PatientProfileValue
```

`PatientProfileValue` uses typed columns such as:

```text
TextValue
NumberValue
BooleanValue
DateValue
```

`JsonValue` is reserved only for genuinely complex values.

Enums stored in the database are persisted as strings, never ordinal integers.

### Dynamic field types

V1 dynamic field types:

```text
Text
LongText / Textarea
Number
Boolean
Date
Image
File
Single Select
Multi Select
```

### Duplicate-patient assistance

Expected behavior:

- proactive similarity search by name/phone,
- hard backend block on exact duplicate phone where applicable.

### Patient number

Patient number is server-generated using the same code-counter foundation:

```text
PREFIX-YEAR-SEQUENCE
```

Only the prefix is configurable by the clinic.

---

## 14. Workflow, Visits and Clinical Orders

### Workflow

V1 uses one clinic-wide dynamic workflow.

### Visits

A visit can contain multiple doctor sessions.

Operational visit completion is separate from clinical documentation completion.

The product should expose a Pending Documentation area/reminder so work can be operationally finished while documentation remains incomplete.

### Follow-up

V1 supports follow-up recommendations.

### Clinical-order / prescription workspace

Prescription is treated as a configurable clinical-order workspace rather than only a medication print form.

It can contain:

- medications,
- tests,
- radiology,
- procedures,
- instructions,
- follow-up.

Files/images can be associated with prescriptions/orders/results through the shared file foundation.

Autocomplete/suggestions may use historical medication/test/radiology values.

---

## 15. Reports, Dashboard and Queue UX Decisions

### Reports

Report workflow:

```text
Choose report
  -> report-specific filters
  -> Preview
  -> Export PDF / Excel
```

### Dashboard

Dashboard is for statistics/charts rather than serving as a general report builder.

### Live Queue

Live Queue should show removable active-filter chips.

A `+ Search / Check In` action is not part of the selected design.

---

## 16. Deferred / Future Scope

Explicitly deferred or future items include:

- appointment scheduling/calendar,
- branches,
- family linking,
- offline sync,
- full SaaS subscriptions/billing,
- platform owner portal beyond current foundation,
- advanced support/break-glass clinical access,
- multi-clinic membership under one human identity unless future requirements demand it.

The current architecture keeps enough foundation for future SaaS evolution without overbuilding those modules now.

---

## 17. API Route Groups

### Platform authentication

```http
POST /api/platform/auth/login
POST /api/platform/auth/refresh
POST /api/platform/auth/logout
```

### Clinic authentication

```http
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
```

### Platform clinic management

```http
GET  /api/platform/clinics
POST /api/platform/clinics
GET  /api/platform/clinics/{id}
PUT  /api/platform/clinics/{id}
PUT  /api/platform/clinics/{id}/status
GET  /api/platform/clinics/{id}/features
PUT  /api/platform/clinics/{id}/features
```

### Clinic self service

```http
GET /api/clinic
GET /api/clinic/settings
PUT /api/clinic/settings
GET /api/clinic/features
```

### Audit

```http
GET /api/platform/audit-logs
GET /api/platform/audit-logs/{id}

GET /api/audit-logs
GET /api/audit-logs/{id}
```

### Reference data

```http
GET /api/reference/fonts
GET /api/reference/countries
GET /api/reference/countries/{countryCode}/cities
GET /api/reference/locales
GET /api/reference/date-formats
GET /api/reference/time-formats
GET /api/reference/time-zones
```

### Health/OpenAPI

```http
GET /health/live
GET /swagger/v1/swagger.json
```

The Swagger JSON endpoint is the OpenAPI document itself and therefore does not appear as a normal operation inside the document it serves.

---

## 18. Manual Authentication Test Contract

### Login and protected request

```text
POST login
  -> A1 + R1

Use A1 on protected endpoint
  -> 200
```

### Refresh rotation

```text
POST refresh using R1
  -> A2 + R2

Use A1
  -> 401

Reuse R1
  -> 401

Use A2
  -> 200
```

### Logout

```text
POST logout using current authenticated session
  -> 200

Use the old access token from that session
  -> 401

Reuse refresh token from that session
  -> 401
```

### Security-scope isolation

Clinic token on Platform route:

```text
403 Forbidden
```

Platform token on Clinic route:

```text
403 Forbidden
```

### Clinic suspension

When a clinic is suspended:

- existing clinic-authenticated sessions must stop working,
- clinic login must fail,
- clinic refresh must fail.

Reactivation restores the ability to establish valid sessions.

---

## 19. Manual Clinic-Provisioning Test Contract

A create-clinic test should verify all of the following:

```text
POST /api/platform/clinics
  -> generated Clinic.Code
  -> Clinic row
  -> ClinicSettings
  -> default ClinicFeatures
  -> Admin/Receptionist/Doctor/Nurse roles
  -> RolePermissions
  -> Identity user
  -> domain User
  -> Admin UserRole
  -> Audit
```

The final clinic code is not supplied manually.

Create two clinics with the same valid prefix and verify distinct sequential generated values.

After creation:

```http
GET /api/platform/clinics/{clinicId}
```

must return the generated code and configuration.

---

## 20. OpenAPI Contract

Swagger/OpenAPI is treated as the machine-readable source of truth for API discovery and future AI/tool integration.

Requirements include:

- stable operation IDs,
- accurate auth requirements,
- anonymous login/refresh/reference operations represented as anonymous,
- descriptions that clarify Platform vs Clinic scopes,
- Swagger UI only in Development,
- Swagger JSON available for contract tooling.

---

## 21. Caching

The application supports:

- Memory cache,
- Redis cache.

Development defaults to Memory unless configured otherwise. Redis remains available for deployment scenarios.

Tenant active/feature checks can use cache, with explicit invalidation after successful platform updates.

---

## 22. Database and Migration Rules

### Audit columns

Domain audit scalar fields use:

```text
CreatedDate
UpdatedDate
CreateByUserId
UpdatedByUserId
```

`CreateByUserId` and `UpdatedByUserId` are scalar audit identifiers, not mandatory physical foreign keys.

### Enum persistence

Enums are persisted as strings, not integers.

### Delete behavior

Domain relationships generally use restrictive delete behavior to avoid accidental cascaded tenant/business data loss.

### PlatformClinicFoundation migration hardening

The migration must preserve existing data safely, including:

- rename old `IsSuperUser` to `IsClinicSuperUser` instead of losing the value,
- existing Identity users become `AccountType = Clinic`,
- existing permissions become `Scope = Clinic`,
- existing clinics remain active,
- legacy audit actor information is snapshotted before old actor relationships are removed,
- legacy global roles are converted into clinic-scoped role copies,
- `UserRole` and `RolePermission` mappings are remapped before non-null clinic foreign keys are enforced,
- no `Guid.Empty` tenant FK placeholder strategy,
- dangerous Down migrations must fail explicitly rather than silently collapsing distinct clinic-specific role data.

### Migration policy

Do not create unnecessary new migrations simply to compensate for an unfinished migration already under review. Harden the current migration in place until the model itself genuinely changes. If the EF model changes after that point, create the required new migration deliberately.

---

## 23. Development / CI Expectations

CI sequence:

```text
Setup .NET 10
Restore
Build
Test
Publish API
```

Temporary workflow logic that generates/commits migrations should not remain in the final production CI workflow after migrations are committed and reviewed.

The repository targets the configured .NET 10 SDK through `global.json`.

---

## 24. Important Security Findings from Manual Testing

The current manual test process surfaced two important issues and drove design changes.

### JWT looked valid but protected endpoints returned invalid_token

Cause category: malformed Authorization header/token presentation rather than refresh-token generation itself.

Resolution:

- normalize Bearer header before validation,
- keep cryptographic JWT validation fully enabled,
- document Swagger behavior clearly.

### Logout initially revoked only refresh token

Observed behavior:

```text
Logout
  -> old access token could still create a clinic
```

This is normal for purely stateless JWTs but did not satisfy the desired security contract.

Resolution:

- introduce server-side session validation using `session_id`,
- revoke the session during logout,
- revoke the previous session during refresh rotation,
- validate session state on protected requests.

Expected result now:

```text
Logout
  -> old access token immediately rejected
  -> old refresh token rejected
```

---

## 25. Historical Implementation Milestones

Major completed foundations before/around the current PR include:

- initial domain/entity model,
- cache abstraction with Memory/Redis,
- .NET 10 and CI foundation,
- initial database migration,
- Authentication/RBAC foundation,
- Platform/Clinic actor separation,
- clinic provisioning,
- central audit foundation,
- server-generated clinic code foundation,
- static reference catalogs,
- access-session revocation,
- file-upload/storage foundation under the same PR review stream.

Current active PR:

```text
PR #5
feat: add clinic management, provisioning and audit foundation
branch: feature/clinic-management-audit-foundation
```

It remains intentionally open for review and manual verification.

---

## 26. Product Prototype Reference

An approved full-demo HTML prototype was produced during the design process:

```text
Clinic_Management_V1_Full_Demo_Prototype_v9.html
```

This prototype is a design reference and should not override backend security/data-ownership rules documented here.

---

## 27. Next Recommended Verification Sequence

Before merging the current foundation PR, verify in order:

1. Database migration on a fresh DB.
2. Database migration against realistic existing clinic/user/role/audit data.
3. Platform bootstrap.
4. Platform login.
5. Platform protected endpoint with access token.
6. Refresh rotation and immediate old-session revocation.
7. Logout and immediate access-token revocation.
8. Create clinic using only `CodePrefix`.
9. Verify generated clinic code and counter concurrency.
10. Verify complete provisioning graph.
11. Reference-data endpoints and validation.
12. Clinic login and own-clinic self-service.
13. Clinic/Platform scope isolation.
14. Clinic suspend/reactivate behavior.
15. Feature enable/disable and cache invalidation.
16. File-upload session create/upload/complete/download flow.
17. Cross-clinic file isolation.
18. Audit redaction and visibility.
19. Swagger/OpenAPI operation IDs and anonymous/protected metadata.
20. Final CI: Restore, Build, Test, Publish.

---

## 28. Non-Negotiable Design Rules

These rules should remain stable unless deliberately revisited:

1. One backend and one frontend for all clinic tenants.
2. Tenant separation is enforced server-side using authenticated clinic context.
3. Platform and Clinic security scopes are separate.
4. Clinic Super User does not mean Platform Admin.
5. Platform Admin does not automatically mean clinical-data access.
6. Business codes are server-generated; the client controls only approved prefix/configuration.
7. Stable dropdown/reference data does not require DB tables in V1.
8. Business entities reference files by stable FileId, not by trusting arbitrary client URLs.
9. Storage provider is abstracted so Local and S3 can use the same frontend/business contract.
10. Audit is central, append-only and tenant-aware.
11. Sensitive values never belong in audit metadata.
12. Enums persisted to SQL use string values.
13. Logout and refresh revoke active sessions immediately.
14. Feature entitlement and user permission are separate concerns.
15. The current PR remains unmerged until explicitly approved.
