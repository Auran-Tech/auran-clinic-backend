# Domain Entities

This branch establishes the V1 persistence model for the Auran Clinic backend.

## Principles

- Clinic business data is clinic-scoped through `ClinicId`.
- System role and permission definitions remain global and stable.
- Configurable workflow statuses are data, not enums.
- Dynamic patient and clinical fields use typed value columns for validation and reporting.
- Visit status and documentation status are independent.
- Queue status history is persisted instead of relying only on the current queue state.
- Multi-session visits are represented by `VisitSession`.
- Clinical orders support structured, text, image, and file sections.

## Entity groups

- Identity & access: User, Role, Permission, UserRole, RolePermission
- Patients: Patient, PatientCondition, PatientAllergy, PatientMedication
- Dynamic profile: PatientProfileSection, PatientProfileField, PatientProfileFieldOption, PatientProfileValue
- Clinical configuration/history: ClinicalField, ClinicalFieldOption, ClinicalMeasurement
- Workflow/queue: WorkflowStatus, WorkflowTransition, QueueEntry, QueueStatusHistory
- Visits/orders: Visit, VisitSession, ClinicalOrderSectionDefinition, ClinicalOrder, ClinicalOrderSection, ClinicalOrderItem
- Files/follow-up/admin: FileRecord, PatientAttachment, ClinicalOrderAttachment, FollowUp, ClinicSettings, AuditLog

No appointment, branch, billing, subscription, or owner-portal entities are introduced in V1.
