using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Domain.Common;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ClinicEntityType = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.Infrastructure.Persistence;

public class AuranClinicDbContext(
    DbContextOptions<AuranClinicDbContext> options,
    ICurrentUserContext currentUserContext)
    : IdentityUserContext<ApplicationIdentityUser>(options)
{
    public DbSet<ClinicEntityType> Clinics => Set<ClinicEntityType>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<PermissionTranslation> PermissionTranslations => Set<PermissionTranslation>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<CodeCounter> CodeCounters => Set<CodeCounter>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientCondition> PatientConditions => Set<PatientCondition>();
    public DbSet<PatientAllergy> PatientAllergies => Set<PatientAllergy>();
    public DbSet<PatientMedication> PatientMedications => Set<PatientMedication>();
    public DbSet<PatientProfileSection> PatientProfileSections => Set<PatientProfileSection>();
    public DbSet<PatientProfileField> PatientProfileFields => Set<PatientProfileField>();
    public DbSet<PatientProfileFieldOption> PatientProfileFieldOptions => Set<PatientProfileFieldOption>();
    public DbSet<PatientProfileValue> PatientProfileValues => Set<PatientProfileValue>();
    public DbSet<PatientProfileValueOption> PatientProfileValueOptions => Set<PatientProfileValueOption>();
    public DbSet<ClinicalField> ClinicalFields => Set<ClinicalField>();
    public DbSet<ClinicalFieldOption> ClinicalFieldOptions => Set<ClinicalFieldOption>();
    public DbSet<ClinicalMeasurement> ClinicalMeasurements => Set<ClinicalMeasurement>();
    public DbSet<WorkflowStatus> WorkflowStatuses => Set<WorkflowStatus>();
    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
    public DbSet<QueueEntry> QueueEntries => Set<QueueEntry>();
    public DbSet<QueueStatusHistory> QueueStatusHistory => Set<QueueStatusHistory>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<VisitSession> VisitSessions => Set<VisitSession>();
    public DbSet<ClinicalOrderSectionDefinition> ClinicalOrderSectionDefinitions => Set<ClinicalOrderSectionDefinition>();
    public DbSet<ClinicalOrder> ClinicalOrders => Set<ClinicalOrder>();
    public DbSet<ClinicalOrderSection> ClinicalOrderSections => Set<ClinicalOrderSection>();
    public DbSet<ClinicalOrderItem> ClinicalOrderItems => Set<ClinicalOrderItem>();
    public DbSet<FileRecord> Files => Set<FileRecord>();
    public DbSet<PatientAttachment> PatientAttachments => Set<PatientAttachment>();
    public DbSet<ClinicalOrderAttachment> ClinicalOrderAttachments => Set<ClinicalOrderAttachment>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<ClinicSettings> ClinicSettings => Set<ClinicSettings>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuranClinicDbContext).Assembly);
        modelBuilder.ConfigureDomainRelationships();
        ApplyClinicQueryFilters(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        PrepareChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepareChanges()
    {
        StampAuditFields();
        EnforceClinicBoundary();
    }

    private void StampAuditFields()
    {
        var now = DateTime.UtcNow;
        var userId = currentUserContext.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                    entry.Entity.Id = Guid.NewGuid();

                if (entry.Entity.CreatedDate == default)
                    entry.Entity.CreatedDate = now;

                entry.Entity.CreateByUserId ??= userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedDate = now;
                entry.Entity.UpdatedByUserId = userId;
            }
        }
    }

    private void EnforceClinicBoundary()
    {
        if (!currentUserContext.IsAuthenticated || currentUserContext.ClinicId is not Guid clinicId)
            return;

        foreach (var entry in ChangeTracker.Entries<ClinicEntity>())
        {
            if (entry.State is EntityState.Detached or EntityState.Unchanged)
                continue;

            if (entry.State == EntityState.Added && entry.Entity.ClinicId == Guid.Empty)
                entry.Entity.ClinicId = clinicId;

            if (entry.Entity.ClinicId != clinicId)
                throw new InvalidOperationException("Cross-clinic data modification is not allowed.");
        }
    }

    private void ApplyClinicQueryFilters(ModelBuilder modelBuilder)
    {
        ApplyClinicQueryFilter<User>(modelBuilder);
        ApplyClinicQueryFilter<UserRole>(modelBuilder);
        ApplyClinicQueryFilter<RefreshToken>(modelBuilder);
        ApplyClinicQueryFilter<CodeCounter>(modelBuilder);
        ApplyClinicQueryFilter<Patient>(modelBuilder);
        ApplyClinicQueryFilter<PatientCondition>(modelBuilder);
        ApplyClinicQueryFilter<PatientAllergy>(modelBuilder);
        ApplyClinicQueryFilter<PatientMedication>(modelBuilder);
        ApplyClinicQueryFilter<PatientProfileSection>(modelBuilder);
        ApplyClinicQueryFilter<PatientProfileField>(modelBuilder);
        ApplyClinicQueryFilter<PatientProfileFieldOption>(modelBuilder);
        ApplyClinicQueryFilter<PatientProfileValue>(modelBuilder);
        ApplyClinicQueryFilter<PatientProfileValueOption>(modelBuilder);
        ApplyClinicQueryFilter<ClinicalField>(modelBuilder);
        ApplyClinicQueryFilter<ClinicalFieldOption>(modelBuilder);
        ApplyClinicQueryFilter<ClinicalMeasurement>(modelBuilder);
        ApplyClinicQueryFilter<WorkflowStatus>(modelBuilder);
        ApplyClinicQueryFilter<WorkflowTransition>(modelBuilder);
        ApplyClinicQueryFilter<QueueEntry>(modelBuilder);
        ApplyClinicQueryFilter<QueueStatusHistory>(modelBuilder);
        ApplyClinicQueryFilter<Visit>(modelBuilder);
        ApplyClinicQueryFilter<VisitSession>(modelBuilder);
        ApplyClinicQueryFilter<ClinicalOrderSectionDefinition>(modelBuilder);
        ApplyClinicQueryFilter<ClinicalOrder>(modelBuilder);
        ApplyClinicQueryFilter<ClinicalOrderSection>(modelBuilder);
        ApplyClinicQueryFilter<ClinicalOrderItem>(modelBuilder);
        ApplyClinicQueryFilter<FileRecord>(modelBuilder);
        ApplyClinicQueryFilter<PatientAttachment>(modelBuilder);
        ApplyClinicQueryFilter<ClinicalOrderAttachment>(modelBuilder);
        ApplyClinicQueryFilter<FollowUp>(modelBuilder);
        ApplyClinicQueryFilter<ClinicSettings>(modelBuilder);
        ApplyClinicQueryFilter<AuditLog>(modelBuilder);
    }

    private void ApplyClinicQueryFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : ClinicEntity
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(entity =>
                currentUserContext.ClinicId.HasValue &&
                entity.ClinicId == currentUserContext.ClinicId.Value);
    }
}
