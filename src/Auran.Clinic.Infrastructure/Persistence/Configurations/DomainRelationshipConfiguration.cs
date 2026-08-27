using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using ClinicEntityType = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public static class DomainRelationshipConfiguration
{
    public static void ConfigureDomainRelationships(this ModelBuilder modelBuilder)
    {
        ConfigureClinicRelationships(modelBuilder);
        ConfigureIdentityAndRbacRelationships(modelBuilder);
        ConfigurePlatformRelationships(modelBuilder);
        ConfigurePatientRelationships(modelBuilder);
        ConfigureClinicalRelationships(modelBuilder);
        ConfigureWorkflowRelationships(modelBuilder);
        ConfigureVisitRelationships(modelBuilder);
        ConfigureFileRelationships(modelBuilder);
    }

    private static void ConfigureIdentityAndRbacRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClinicSettings>()
            .HasOne<ClinicEntityType>()
            .WithOne()
            .HasForeignKey<ClinicSettings>(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasOne<ApplicationIdentityUser>()
            .WithOne()
            .HasForeignKey<User>(x => x.IdentityUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserRole>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

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

        modelBuilder.Entity<RefreshToken>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurePlatformRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlatformUser>()
            .HasOne<ApplicationIdentityUser>()
            .WithOne()
            .HasForeignKey<PlatformUser>(x => x.IdentityUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PlatformUserRole>()
            .HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.PlatformUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PlatformUserRole>()
            .HasOne<PlatformRole>()
            .WithMany()
            .HasForeignKey(x => x.PlatformRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PlatformRolePermission>()
            .HasOne<PlatformRole>()
            .WithMany()
            .HasForeignKey(x => x.PlatformRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PlatformRolePermission>()
            .HasOne<Permission>()
            .WithMany()
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PlatformRefreshToken>()
            .HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.PlatformUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClinicFeature>()
            .HasOne<FeatureDefinition>()
            .WithMany()
            .HasForeignKey(x => x.FeatureDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureClinicRelationships(ModelBuilder modelBuilder)
    {
        ConfigureClinic<User>(modelBuilder);
        ConfigureClinic<UserRole>(modelBuilder);
        ConfigureClinic<RefreshToken>(modelBuilder);
        ConfigureClinic<ClinicFeature>(modelBuilder);
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

    private static void ConfigurePatientRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PatientCondition>()
            .HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PatientCondition>()
            .HasOne<User>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientAllergy>()
            .HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PatientAllergy>()
            .HasOne<User>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientMedication>()
            .HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PatientMedication>()
            .HasOne<User>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientProfileField>()
            .HasOne<PatientProfileSection>().WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PatientProfileFieldOption>()
            .HasOne<PatientProfileField>().WithMany().HasForeignKey(x => x.FieldId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PatientProfileValue>()
            .HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PatientProfileValue>()
            .HasOne<PatientProfileField>().WithMany().HasForeignKey(x => x.FieldId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PatientProfileValue>()
            .HasOne<FileRecord>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureClinicalRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClinicalFieldOption>()
            .HasOne<ClinicalField>().WithMany().HasForeignKey(x => x.ClinicalFieldId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClinicalMeasurement>()
            .HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ClinicalMeasurement>()
            .HasOne<Visit>().WithMany().HasForeignKey(x => x.VisitId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ClinicalMeasurement>()
            .HasOne<ClinicalField>().WithMany().HasForeignKey(x => x.ClinicalFieldId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ClinicalMeasurement>()
            .HasOne<User>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClinicalOrder>()
            .HasOne<Visit>().WithMany().HasForeignKey(x => x.VisitId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ClinicalOrder>()
            .HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ClinicalOrder>()
            .HasOne<User>().WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ClinicalOrder>()
            .HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClinicalOrderSection>()
            .HasOne<ClinicalOrder>().WithMany().HasForeignKey(x => x.ClinicalOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ClinicalOrderSection>()
            .HasOne<ClinicalOrderSectionDefinition>().WithMany().HasForeignKey(x => x.SectionDefinitionId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ClinicalOrderItem>()
            .HasOne<ClinicalOrderSection>().WithMany().HasForeignKey(x => x.ClinicalOrderSectionId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClinicalOrderAttachment>()
            .HasOne<ClinicalOrder>().WithMany().HasForeignKey(x => x.ClinicalOrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ClinicalOrderAttachment>()
            .HasOne<ClinicalOrderSection>().WithMany().HasForeignKey(x => x.ClinicalOrderSectionId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ClinicalOrderAttachment>()
            .HasOne<FileRecord>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureWorkflowRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowTransition>()
            .HasOne<WorkflowStatus>().WithMany().HasForeignKey(x => x.FromStatusId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WorkflowTransition>()
            .HasOne<WorkflowStatus>().WithMany().HasForeignKey(x => x.ToStatusId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<QueueEntry>()
            .HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<QueueEntry>()
            .HasOne<Visit>().WithMany().HasForeignKey(x => x.VisitId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<QueueEntry>()
            .HasOne<User>().WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<QueueEntry>()
            .HasOne<WorkflowStatus>().WithMany().HasForeignKey(x => x.WorkflowStatusId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<QueueStatusHistory>()
            .HasOne<QueueEntry>().WithMany().HasForeignKey(x => x.QueueEntryId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<QueueStatusHistory>()
            .HasOne<WorkflowStatus>().WithMany().HasForeignKey(x => x.FromStatusId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<QueueStatusHistory>()
            .HasOne<WorkflowStatus>().WithMany().HasForeignKey(x => x.ToStatusId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<QueueStatusHistory>()
            .HasOne<User>().WithMany().HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureVisitRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Visit>()
            .HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Visit>()
            .HasOne<User>().WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VisitSession>()
            .HasOne<Visit>().WithMany().HasForeignKey(x => x.VisitId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<VisitSession>()
            .HasOne<User>().WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<VisitSession>()
            .HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FollowUp>()
            .HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FollowUp>()
            .HasOne<Visit>().WithMany().HasForeignKey(x => x.VisitId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FollowUp>()
            .HasOne<User>().WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureFileRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileRecord>()
            .HasOne<User>().WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PatientAttachment>()
            .HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PatientAttachment>()
            .HasOne<FileRecord>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
    }
}
