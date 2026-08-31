using System.Reflection;
using Auran.Clinic.Application.Abstractions;
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
    : IdentityDbContext<ApplicationIdentityUser>(options)
{
    private static readonly MethodInfo ApplyClinicQueryFilterMethod = typeof(AuranClinicDbContext)
        .GetMethod(nameof(ApplyClinicQueryFilter), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private bool EnforceClinicScope => currentUserContext.IsAuthenticated && currentUserContext.ClinicId.HasValue;
    private Guid CurrentClinicId => currentUserContext.ClinicId ?? Guid.Empty;

    public DbSet<ClinicEntityType> Clinics => Set<ClinicEntityType>();
    public new DbSet<User> Users => Set<User>();
    public new DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public new DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientCondition> PatientConditions => Set<PatientCondition>();
    public DbSet<PatientAllergy> PatientAllergies => Set<PatientAllergy>();
    public DbSet<PatientMedication> PatientMedications => Set<PatientMedication>();
    public DbSet<PatientProfileSection> PatientProfileSections => Set<PatientProfileSection>();
    public DbSet<PatientProfileField> PatientProfileFields => Set<PatientProfileField>();
    public DbSet<PatientProfileFieldOption> PatientProfileFieldOptions => Set<PatientProfileFieldOption>();
    public DbSet<PatientProfileValue> PatientProfileValues => Set<PatientProfileValue>();
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
        EnforceClinicWriteBoundary();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnforceClinicWriteBoundary();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyClinicQueryFilters(ModelBuilder modelBuilder)
    {
        var clinicEntityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(entityType =>
                typeof(ClinicEntity).IsAssignableFrom(entityType.ClrType) &&
                !entityType.ClrType.IsAbstract)
            .Select(entityType => entityType.ClrType)
            .Distinct()
            .ToList();

        foreach (var clinicEntityType in clinicEntityTypes)
        {
            ApplyClinicQueryFilterMethod
                .MakeGenericMethod(clinicEntityType)
                .Invoke(this, [modelBuilder]);
        }
    }

    private void ApplyClinicQueryFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : ClinicEntity
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(entity => !EnforceClinicScope || entity.ClinicId == CurrentClinicId);
    }

    private void EnforceClinicWriteBoundary()
    {
        if (!EnforceClinicScope)
            return;

        var currentClinicId = CurrentClinicId;

        foreach (var entry in ChangeTracker.Entries<ClinicEntity>()
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.ClinicId == Guid.Empty)
                {
                    entry.Entity.ClinicId = currentClinicId;
                    continue;
                }

                if (entry.Entity.ClinicId != currentClinicId)
                    throw new InvalidOperationException("Cross-clinic write access is not allowed.");

                continue;
            }

            var originalClinicId = entry.Property(entity => entity.ClinicId).OriginalValue;
            if (originalClinicId != currentClinicId || entry.Entity.ClinicId != currentClinicId)
                throw new InvalidOperationException("Cross-clinic write access is not allowed.");
        }
    }
}
