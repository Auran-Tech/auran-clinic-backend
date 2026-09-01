using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class WorkflowVisitTenantIntegrityConfiguration :
    IEntityTypeConfiguration<WorkflowStatus>,
    IEntityTypeConfiguration<WorkflowTransition>,
    IEntityTypeConfiguration<QueueEntry>,
    IEntityTypeConfiguration<QueueStatusHistory>,
    IEntityTypeConfiguration<Visit>,
    IEntityTypeConfiguration<VisitSession>,
    IEntityTypeConfiguration<FollowUp>,
    IEntityTypeConfiguration<ClinicalField>,
    IEntityTypeConfiguration<ClinicalFieldOption>,
    IEntityTypeConfiguration<ClinicalMeasurement>,
    IEntityTypeConfiguration<ClinicalOrder>,
    IEntityTypeConfiguration<ClinicalOrderSectionDefinition>,
    IEntityTypeConfiguration<ClinicalOrderSection>,
    IEntityTypeConfiguration<ClinicalOrderItem>,
    IEntityTypeConfiguration<ClinicalOrderAttachment>
{
    public void Configure(EntityTypeBuilder<WorkflowStatus> builder) =>
        builder.HasAlternateKey(entity => new { entity.Id, entity.ClinicId });

    public void Configure(EntityTypeBuilder<WorkflowTransition> builder)
    {
        builder.HasOne<WorkflowStatus>()
            .WithMany()
            .HasForeignKey(entity => new { entity.FromStatusId, entity.ClinicId })
            .HasPrincipalKey(status => new { status.Id, status.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WorkflowStatus>()
            .WithMany()
            .HasForeignKey(entity => new { entity.ToStatusId, entity.ClinicId })
            .HasPrincipalKey(status => new { status.Id, status.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<QueueEntry> builder)
    {
        builder.HasAlternateKey(entity => new { entity.Id, entity.ClinicId });

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(entity => new { entity.PatientId, entity.ClinicId })
            .HasPrincipalKey(patient => new { patient.Id, patient.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Visit>()
            .WithMany()
            .HasForeignKey(entity => new { entity.VisitId, entity.ClinicId })
            .HasPrincipalKey(visit => new { visit.Id, visit.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => new { entity.DoctorId, entity.ClinicId })
            .HasPrincipalKey(user => new { user.Id, user.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WorkflowStatus>()
            .WithMany()
            .HasForeignKey(entity => new { entity.WorkflowStatusId, entity.ClinicId })
            .HasPrincipalKey(status => new { status.Id, status.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<QueueStatusHistory> builder)
    {
        builder.HasOne<QueueEntry>()
            .WithMany()
            .HasForeignKey(entity => new { entity.QueueEntryId, entity.ClinicId })
            .HasPrincipalKey(queue => new { queue.Id, queue.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WorkflowStatus>()
            .WithMany()
            .HasForeignKey(entity => new { entity.FromStatusId, entity.ClinicId })
            .HasPrincipalKey(status => new { status.Id, status.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WorkflowStatus>()
            .WithMany()
            .HasForeignKey(entity => new { entity.ToStatusId, entity.ClinicId })
            .HasPrincipalKey(status => new { status.Id, status.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => new { entity.ChangedByUserId, entity.ClinicId })
            .HasPrincipalKey(user => new { user.Id, user.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.HasAlternateKey(entity => new { entity.Id, entity.ClinicId });

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(entity => new { entity.PatientId, entity.ClinicId })
            .HasPrincipalKey(patient => new { patient.Id, patient.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => new { entity.DoctorId, entity.ClinicId })
            .HasPrincipalKey(user => new { user.Id, user.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<VisitSession> builder)
    {
        builder.HasOne<Visit>()
            .WithMany()
            .HasForeignKey(entity => new { entity.VisitId, entity.ClinicId })
            .HasPrincipalKey(visit => new { visit.Id, visit.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => new { entity.DoctorId, entity.ClinicId })
            .HasPrincipalKey(user => new { user.Id, user.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => new { entity.CreatedByUserId, entity.ClinicId })
            .HasPrincipalKey(user => new { user.Id, user.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<FollowUp> builder)
    {
        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(entity => new { entity.PatientId, entity.ClinicId })
            .HasPrincipalKey(patient => new { patient.Id, patient.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Visit>()
            .WithMany()
            .HasForeignKey(entity => new { entity.VisitId, entity.ClinicId })
            .HasPrincipalKey(visit => new { visit.Id, visit.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => new { entity.DoctorId, entity.ClinicId })
            .HasPrincipalKey(user => new { user.Id, user.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<ClinicalField> builder) =>
        builder.HasAlternateKey(entity => new { entity.Id, entity.ClinicId });

    public void Configure(EntityTypeBuilder<ClinicalFieldOption> builder) =>
        builder.HasOne<ClinicalField>()
            .WithMany()
            .HasForeignKey(entity => new { entity.ClinicalFieldId, entity.ClinicId })
            .HasPrincipalKey(field => new { field.Id, field.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

    public void Configure(EntityTypeBuilder<ClinicalMeasurement> builder)
    {
        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(entity => new { entity.PatientId, entity.ClinicId })
            .HasPrincipalKey(patient => new { patient.Id, patient.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Visit>()
            .WithMany()
            .HasForeignKey(entity => new { entity.VisitId, entity.ClinicId })
            .HasPrincipalKey(visit => new { visit.Id, visit.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ClinicalField>()
            .WithMany()
            .HasForeignKey(entity => new { entity.ClinicalFieldId, entity.ClinicId })
            .HasPrincipalKey(field => new { field.Id, field.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => new { entity.RecordedByUserId, entity.ClinicId })
            .HasPrincipalKey(user => new { user.Id, user.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<ClinicalOrder> builder)
    {
        builder.HasAlternateKey(entity => new { entity.Id, entity.ClinicId });

        builder.HasOne<Visit>()
            .WithMany()
            .HasForeignKey(entity => new { entity.VisitId, entity.ClinicId })
            .HasPrincipalKey(visit => new { visit.Id, visit.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(entity => new { entity.PatientId, entity.ClinicId })
            .HasPrincipalKey(patient => new { patient.Id, patient.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => new { entity.DoctorId, entity.ClinicId })
            .HasPrincipalKey(user => new { user.Id, user.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entity => new { entity.CreatedByUserId, entity.ClinicId })
            .HasPrincipalKey(user => new { user.Id, user.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<ClinicalOrderSectionDefinition> builder) =>
        builder.HasAlternateKey(entity => new { entity.Id, entity.ClinicId });

    public void Configure(EntityTypeBuilder<ClinicalOrderSection> builder)
    {
        builder.HasAlternateKey(entity => new { entity.Id, entity.ClinicId });

        builder.HasOne<ClinicalOrder>()
            .WithMany()
            .HasForeignKey(entity => new { entity.ClinicalOrderId, entity.ClinicId })
            .HasPrincipalKey(order => new { order.Id, order.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ClinicalOrderSectionDefinition>()
            .WithMany()
            .HasForeignKey(entity => new { entity.SectionDefinitionId, entity.ClinicId })
            .HasPrincipalKey(definition => new { definition.Id, definition.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<ClinicalOrderItem> builder) =>
        builder.HasOne<ClinicalOrderSection>()
            .WithMany()
            .HasForeignKey(entity => new { entity.ClinicalOrderSectionId, entity.ClinicId })
            .HasPrincipalKey(section => new { section.Id, section.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

    public void Configure(EntityTypeBuilder<ClinicalOrderAttachment> builder)
    {
        builder.HasOne<ClinicalOrder>()
            .WithMany()
            .HasForeignKey(entity => new { entity.ClinicalOrderId, entity.ClinicId })
            .HasPrincipalKey(order => new { order.Id, order.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ClinicalOrderSection>()
            .WithMany()
            .HasForeignKey(entity => new { entity.ClinicalOrderSectionId, entity.ClinicId })
            .HasPrincipalKey(section => new { section.Id, section.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FileRecord>()
            .WithMany()
            .HasForeignKey(entity => new { entity.FileId, entity.ClinicId })
            .HasPrincipalKey(file => new { file.Id, file.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
