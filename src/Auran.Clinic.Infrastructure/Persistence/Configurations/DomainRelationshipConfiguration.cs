using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ClinicEntityType = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public static class DomainRelationshipConfiguration
{
    public static void ConfigureDomainRelationships(this ModelBuilder modelBuilder)
    {
        ConfigureClinicRelationships(modelBuilder);

        modelBuilder.Entity<ClinicSettings>()
            .HasOne<ClinicEntityType>()
            .WithOne()
            .HasForeignKey<ClinicSettings>(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        // User -> Identity is configured by UserConfiguration with the account-type discriminator.
        // UserRole -> User and RefreshToken -> User are tenant-safe composite relationships
        // configured by their dedicated entity configurations.
        modelBuilder.Entity<UserRole>()
            .HasOne<Role>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RolePermission>()
            .HasOne<Role>()
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RolePermission>()
            .HasOne<Permission>()
            .WithMany()
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Patient/profile/file relationships are configured by PatientTenantIntegrityConfiguration.
        // Workflow, queue, visit and clinical-order relationships are configured by
        // WorkflowVisitTenantIntegrityConfiguration using composite tenant foreign keys.
        // AuditLog -> User is configured by AuditLogConfiguration using a composite tenant FK.
    }

    private static void ConfigureClinicRelationships(ModelBuilder modelBuilder)
    {
        ConfigureClinic<User>(modelBuilder);
        ConfigureClinic<UserRole>(modelBuilder);
        ConfigureClinic<RefreshToken>(modelBuilder);
        ConfigureClinic<Patient>(modelBuilder);
        ConfigureClinic<PatientCondition>(modelBuilder);
        ConfigureClinic<PatientAllergy>(modelBuilder);
        ConfigureClinic<PatientMedication>(modelBuilder);
        ConfigureClinic<PatientProfileSection>(modelBuilder);
        ConfigureClinic<PatientProfileField>(modelBuilder);
        ConfigureClinic<PatientProfileFieldOption>(modelBuilder);
        ConfigureClinic<PatientProfileValue>(modelBuilder);
        ConfigureClinic<ClinicalField>(modelBuilder);
        ConfigureClinic<ClinicalFieldOption>(modelBuilder);
        ConfigureClinic<ClinicalMeasurement>(modelBuilder);
        ConfigureClinic<WorkflowStatus>(modelBuilder);
        ConfigureClinic<WorkflowTransition>(modelBuilder);
        ConfigureClinic<QueueEntry>(modelBuilder);
        ConfigureClinic<QueueStatusHistory>(modelBuilder);
        ConfigureClinic<Visit>(modelBuilder);
        ConfigureClinic<VisitSession>(modelBuilder);
        ConfigureClinic<ClinicalOrderSectionDefinition>(modelBuilder);
        ConfigureClinic<ClinicalOrder>(modelBuilder);
        ConfigureClinic<ClinicalOrderSection>(modelBuilder);
        ConfigureClinic<ClinicalOrderItem>(modelBuilder);
        ConfigureClinic<FileRecord>(modelBuilder);
        ConfigureClinic<PatientAttachment>(modelBuilder);
        ConfigureClinic<ClinicalOrderAttachment>(modelBuilder);
        ConfigureClinic<FollowUp>(modelBuilder);
        ConfigureClinic<AuditLog>(modelBuilder);
    }

    private static void ConfigureClinic<TEntity>(ModelBuilder modelBuilder)
        where TEntity : ClinicEntity
    {
        modelBuilder.Entity<TEntity>()
            .HasOne<ClinicEntityType>()
            .WithMany()
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
