# Entity Design Conventions

This file is replaced by the complete domain model document in this PR. It exists only to make the entity conventions explicit during review.

- Every class and every enum is stored in its own file.
- All entities inherit audit fields from `BaseEntity`: `CreatedDate`, `UpdatedDate`, `CreateByUserId`, and `UpdatedByUserId`.
- Date/time audit values are stored in UTC even though the property names intentionally omit the `Utc` suffix.
- Enum properties are persisted as readable strings through explicit EF Core conversions, never as numeric ordinals.
- Clinic-owned business data inherits `ClinicEntity` and is scoped by `ClinicId`.
