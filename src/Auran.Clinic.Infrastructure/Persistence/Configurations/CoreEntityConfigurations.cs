using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasIndex(x => new { x.ClinicId, x.PatientNumber }).IsUnique();
        builder.HasIndex(x => new { x.ClinicId, x.Phone }).IsUnique();
        builder.Property(x => x.PatientNumber).HasMaxLength(64);
        builder.Property(x => x.FullName).HasMaxLength(256);
        builder.Property(x => x.Phone).HasMaxLength(64);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Code).HasMaxLength(128);
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Code).HasMaxLength(160);
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder) =>
        builder.HasIndex(x => new { x.ClinicId, x.UserId, x.RoleId }).IsUnique();
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder) =>
        builder.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
}

public class WorkflowStatusConfiguration : IEntityTypeConfiguration<WorkflowStatus>
{
    public void Configure(EntityTypeBuilder<WorkflowStatus> builder)
    {
        builder.HasIndex(x => new { x.ClinicId, x.Code }).IsUnique();
        builder.Property(x => x.Color).HasMaxLength(32);
    }
}

public class WorkflowTransitionConfiguration : IEntityTypeConfiguration<WorkflowTransition>
{
    public void Configure(EntityTypeBuilder<WorkflowTransition> builder) =>
        builder.HasIndex(x => new { x.ClinicId, x.FromStatusId, x.ToStatusId }).IsUnique();
}

public class QueueEntryConfiguration : IEntityTypeConfiguration<QueueEntry>
{
    public void Configure(EntityTypeBuilder<QueueEntry> builder)
    {
        builder.HasIndex(x => new { x.ClinicId, x.VisitId });
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

public class ClinicSettingsConfiguration : IEntityTypeConfiguration<ClinicSettings>
{
    public void Configure(EntityTypeBuilder<ClinicSettings> builder) =>
        builder.HasIndex(x => x.ClinicId).IsUnique();
}
