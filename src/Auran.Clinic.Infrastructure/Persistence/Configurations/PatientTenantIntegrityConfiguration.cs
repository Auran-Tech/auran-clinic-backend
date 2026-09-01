using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class PatientTenantIntegrityConfiguration :
    IEntityTypeConfiguration<Patient>,
    IEntityTypeConfiguration<PatientCondition>,
    IEntityTypeConfiguration<PatientAllergy>,
    IEntityTypeConfiguration<PatientMedication>,
    IEntityTypeConfiguration<PatientProfileSection>,
    IEntityTypeConfiguration<PatientProfileField>,
    IEntityTypeConfiguration<PatientProfileFieldOption>,
    IEntityTypeConfiguration<PatientProfileValue>,
    IEntityTypeConfiguration<FileRecord>,
    IEntityTypeConfiguration<PatientAttachment>
{
    public void Configure(EntityTypeBuilder<Patient> builder) =>
        builder.HasAlternateKey(entity => new { entity.Id, entity.ClinicId });

    public void Configure(EntityTypeBuilder<PatientCondition> builder)
    {
        ConfigurePatientRelationship(builder, condition => condition.PatientId);
        ConfigureUserRelationship(builder, condition => condition.RecordedByUserId);
    }

    public void Configure(EntityTypeBuilder<PatientAllergy> builder)
    {
        ConfigurePatientRelationship(builder, allergy => allergy.PatientId);
        ConfigureUserRelationship(builder, allergy => allergy.RecordedByUserId);
    }

    public void Configure(EntityTypeBuilder<PatientMedication> builder)
    {
        ConfigurePatientRelationship(builder, medication => medication.PatientId);
        ConfigureUserRelationship(builder, medication => medication.RecordedByUserId);
    }

    public void Configure(EntityTypeBuilder<PatientProfileSection> builder) =>
        builder.HasAlternateKey(entity => new { entity.Id, entity.ClinicId });

    public void Configure(EntityTypeBuilder<PatientProfileField> builder)
    {
        builder.HasAlternateKey(entity => new { entity.Id, entity.ClinicId });

        builder.HasOne<PatientProfileSection>()
            .WithMany()
            .HasForeignKey(field => new { field.SectionId, field.ClinicId })
            .HasPrincipalKey(section => new { section.Id, section.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<PatientProfileFieldOption> builder)
    {
        builder.HasOne<PatientProfileField>()
            .WithMany()
            .HasForeignKey(option => new { option.FieldId, option.ClinicId })
            .HasPrincipalKey(field => new { field.Id, field.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<PatientProfileValue> builder)
    {
        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(value => new { value.PatientId, value.ClinicId })
            .HasPrincipalKey(patient => new { patient.Id, patient.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PatientProfileField>()
            .WithMany()
            .HasForeignKey(value => new { value.FieldId, value.ClinicId })
            .HasPrincipalKey(field => new { field.Id, field.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FileRecord>()
            .WithMany()
            .HasForeignKey(value => new { value.FileId, value.ClinicId })
            .HasPrincipalKey(file => new { file.Id, file.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<FileRecord> builder)
    {
        builder.HasAlternateKey(entity => new { entity.Id, entity.ClinicId });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(file => new { file.UploadedByUserId, file.ClinicId })
            .HasPrincipalKey(user => new { user.Id, user.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<PatientAttachment> builder)
    {
        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(attachment => new { attachment.PatientId, attachment.ClinicId })
            .HasPrincipalKey(patient => new { patient.Id, patient.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FileRecord>()
            .WithMany()
            .HasForeignKey(attachment => new { attachment.FileId, attachment.ClinicId })
            .HasPrincipalKey(file => new { file.Id, file.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurePatientRelationship<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        System.Linq.Expressions.Expression<Func<TEntity, Guid>> patientId)
        where TEntity : ClinicEntity
    {
        var patientProperty = patientId.GetMemberAccess();
        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(patientProperty, builder.Metadata.FindProperty(nameof(ClinicEntity.ClinicId))!)
            .HasPrincipalKey<Patient>(nameof(Patient.Id), nameof(ClinicEntity.ClinicId))
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureUserRelationship<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        System.Linq.Expressions.Expression<Func<TEntity, Guid>> userId)
        where TEntity : ClinicEntity
    {
        var userProperty = userId.GetMemberAccess();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(userProperty, builder.Metadata.FindProperty(nameof(ClinicEntity.ClinicId))!)
            .HasPrincipalKey<User>(nameof(User.Id), nameof(ClinicEntity.ClinicId))
            .OnDelete(DeleteBehavior.Restrict);
    }
}
