# Auran Clinic — Current Domain & Database Model

> Scope: current backend foundation and V1 domain persistence.  
> Runtime: .NET 10 + EF Core 10 + SQL Server.  
> Tenancy: one database, clinic-owned rows scoped by `ClinicId`.

## Core entity conventions

`BaseEntity` provides the shared audit identity fields:

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public Guid? CreateByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
```

Clinic-owned data derives from:

```csharp
public abstract class ClinicEntity : BaseEntity
{
    public Guid ClinicId { get; set; }
}
```

For authenticated Clinic actors, `AuranClinicDbContext` applies global query filters to all `ClinicEntity` types and rejects cross-clinic writes during `SaveChanges`.

Enums controlled by application code are persisted as readable strings.

## Identity and security model

ASP.NET Core Identity is the credential store only. Application RBAC is represented by AURAN domain entities rather than `AspNetRoles`.

```text
ApplicationIdentityUser
  AccountType = Platform | Clinic

Platform account
  -> PlatformUser
  -> PlatformUserRole
  -> PlatformRole
  -> PlatformRolePermission
  -> Permission(scope=Platform)

Clinic account
  -> User(ClinicId)
  -> UserRole(ClinicId)
  -> Role(ClinicId)
  -> RolePermission(ClinicId)
  -> Permission(scope=Clinic)
```

### User

Important fields:

```text
ClinicId
IdentityUserId
FullName
Email
Phone
IsActive
IsClinicSuperUser
```

`IsActive` is the business-account state and is separate from ASP.NET Identity's temporary authentication lockout.

`IsClinicSuperUser` is protected user state, not an ordinary role. The backend calculates its effective permissions as every clinic-scoped permission.

### Clinic

Important foundation fields include:

```text
Name
Code
IsActive
LogoUrl
PrimaryColor
SecondaryColor
FontFamily
WelcomeTitle
WelcomeMessage
TimeZoneId
PatientNumberPrefix
```

`Clinic.IsActive=false` suspends the complete tenant.

### Role

Clinic roles are tenant-scoped and protected system roles are created during provisioning:

```text
Admin
Receptionist
Doctor
Nurse
```

A user may have multiple roles. Effective normal-user permissions are the union of all role-permission mappings.

### Permission

Permission identity is global, stable, language-independent data:

```text
Key
GroupKey
Scope = Clinic | Platform
```

Examples:

```text
Patient_View
Patient_Create
Users_Manage_Status
Queue_Move
Visit_Edit
Reports_View
Settings_Manage
Platform_Clinics_Create
```

### PermissionTranslation

Localized descriptions are stored separately:

```text
PermissionId
LanguageCode
Description
```

Database uniqueness:

```text
UNIQUE (PermissionId, LanguageCode)
```

English and Arabic are seeded. Adding another language does not change the `Permission` schema.

## Authentication sessions

Clinic and Platform refresh sessions use separate entities:

```text
RefreshToken
PlatformRefreshToken
```

Raw refresh tokens are never stored. SHA-256 hashes are persisted.

The refresh-token row id is also the access-token `session_id`, enabling immediate access-session revocation after refresh/logout or account/clinic disable checks.

## Platform model

### PlatformUser

AURAN-side operator identity. It is not assigned to a fake clinic.

### PlatformRole / PlatformUserRole / PlatformRolePermission

Separate platform RBAC graph used for tenant lifecycle operations.

### FeatureDefinition / ClinicFeature

Global feature catalog plus clinic-specific enable/configuration state. Feature entitlement is independent from user permission.

## Business-code generation

### CodeCounter

General backend-generated sequence state:

```text
Scope        Platform | Clinic
ClinicId     nullable (required for Clinic scope)
CodeType     Clinic | Patient | future generated types
Prefix
Year
LastNumber  bigint
```

Generated format:

```text
PREFIX-YEAR-SEQUENCE
```

SQL Server transactions, `UPDLOCK/HOLDLOCK`, scope check constraints, and filtered unique indexes protect concurrent generation.

## Patient model

### Patient

Universal patient identity/basic information:

```text
ClinicId
PatientNumber
FullName
Phone
Gender
DateOfBirth
Notes
```

Important uniqueness:

```text
UNIQUE (ClinicId, PatientNumber)
UNIQUE (ClinicId, Phone)
```

Specialty-specific clinical/profile data does not become hard-coded columns on `Patient`.

### PatientCondition / PatientAllergy / PatientMedication

Structured medical-profile records associated with the patient.

## Dynamic patient profile

Configuration:

```text
PatientProfileSection
  -> PatientProfileField
     -> PatientProfileFieldOption
```

Patient values:

```text
PatientProfileValue
  TextValue
  NumberValue
  BooleanValue
  DateValue
  FileId
```

One current value is enforced per:

```text
UNIQUE (ClinicId, PatientId, FieldId)
```

Multi-select data is normalized through:

```text
PatientProfileValueOption
  ClinicId
  PatientProfileValueId
  OptionId
```

This replaces JSON-array storage for multi-select values and keeps reporting/filtering relational.

## Clinical fields and measurements

```text
ClinicalField
ClinicalFieldOption
ClinicalMeasurement
```

`ClinicalMeasurement` is historical/append-style data associated with a patient and optionally a visit. New measurements do not overwrite old measurements.

## Workflow and queue

```text
WorkflowStatus
WorkflowTransition
QueueEntry
QueueStatusHistory
```

Workflow statuses/transitions are clinic-configurable data rather than enums.

`QueueEntry` represents the current queue state for a visit and contains SQL Server `rowversion` concurrency data.

One queue entry is enforced per:

```text
UNIQUE (ClinicId, VisitId)
```

`QueueStatusHistory` preserves transitions for reporting and traceability.

## Visits and sessions

### Visit

Represents one patient encounter and includes SQL Server `rowversion` concurrency protection.

Follow-up information is not duplicated as free text on Visit; the dedicated `FollowUp` entity is the source of truth.

### VisitSession

A visit may contain multiple completed doctor sessions over time, but only one active session is allowed at a time:

```text
UNIQUE (ClinicId, VisitId)
WHERE EndedAtUtc IS NULL
```

## Clinical orders

```text
ClinicalOrderSectionDefinition
ClinicalOrder
ClinicalOrderSection
ClinicalOrderItem
ClinicalOrderAttachment
```

The structure supports configurable prescription/order sections without customer-specific schemas.

## Files

### FileRecord

Permanent clinic-owned metadata registry. Business entities should store `FileId` where possible rather than permanent external URLs.

### FileUploadSession

Short-lived upload workflow state:

```text
ClinicId
RequestedByActorType
RequestedByActorId
OriginalName
ContentType
ExpectedSize
StorageProvider
StorageKey
UploadTokenHash
Status
ExpiresAtUtc
UploadedAtUtc
CompletedAtUtc
FileId
```

The raw upload token is not stored.

Attachments include `PatientAttachment` and `ClinicalOrderAttachment`.

## Follow-up

`FollowUp` is the dedicated patient/visit follow-up entity and the single source for follow-up recommendations/state.

## Clinic settings

`ClinicSettings` stores operational/localization/contact configuration associated one-to-one with a Clinic.

## Audit

`AuditLog` is append-only and supports both Platform and Clinic scopes.

It records actor snapshots, action/category, entity information, metadata, request/network context, and occurrence time. Secrets and authentication credentials are redacted.

## Database invariants summary

The current foundation explicitly protects:

- tenant query isolation,
- tenant write isolation,
- clinic/account active state,
- unique patient number and phone per clinic,
- one queue entry per visit,
- one active visit session per visit,
- one profile value per patient/field,
- normalized unique multi-select selections,
- permission translation uniqueness,
- visit/queue optimistic concurrency,
- atomic generated-code sequences,
- separation of Platform and Clinic RBAC,
- removal of unused ASP.NET Identity role tables from the current model.

Schema/model drift is checked automatically in CI using EF Core migrations against SQL Server.
