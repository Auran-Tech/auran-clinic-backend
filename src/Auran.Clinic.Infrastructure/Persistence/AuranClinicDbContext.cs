using System.Reflection;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ClinicEntityType = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.Infrastructure.Persistence;

public class AuranClinicDbContext(
    DbContextOptions<AuranClinicDbContext> options,
    ICurrentActor currentActor)
    : IdentityUserContext<ApplicationIdentityUser>(options)
{
    private static readonly MethodInfo ConfigureClinicFilterMethod = typeof(AuranClinicDbContext)
        .GetMethod(nameof(ConfigureClinicFilter), BindingFlags.Instance | BindingFlags.NonPublic)!;

    public bool EnforceClinicScope =>
        currentActor.IsAuthenticated
        && currentActor.ActorType == ActorType.Clinic
        && currentActor.ClinicId.HasValue;

    public Guid? CurrentClinicId => currentActor.ClinicId;

    public DbSet<ClinicEntityType> Clinics => Set<ClinicEntityType>();
    public new DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<PermissionTranslation> PermissionTranslations => Set<PermissionTranslation>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<CodeCounter> CodeCounters => Set<CodeCounter>();

    public DbSet<PlatformUser> PlatformUsers => Set<PlatformUser>();
    public DbSet<PlatformRole> PlatformRoles => Set<PlatformRole>();
    public DbSet<PlatformUserRole> PlatformUserRoles => Set<PlatformUserRole>();
    public DbSet<PlatformRolePermission> PlatformRolePermissions => Set<PlatformRolePermission>();
    public DbSet<PlatformRefreshToken> PlatformRefreshTokens => Set<PlatformRefreshToken>();

    public DbSet<FeatureDefinition> FeatureDefinitions => Set<FeatureDefinition>();
    public DbSet<ClinicFeature> ClinicFeatures => Set<ClinicFeature>();

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
    public DbSet<FileUploadSession> FileUploadSessions => Set<FileUploadSession>();
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
            .Where(x => typeof(ClinicEntity).IsAssignableFrom(x.ClrType))
            .Select(x => x.ClrType)
            .Distinct()
            .ToArray();

        foreach (var entityType in clinicEntityTypes)
            ConfigureClinicFilterMethod.MakeGenericMethod(entityType).Invoke(this, [modelBuilder]);
    }

    private void ConfigureClinicFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : ClinicEntity
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(entity => !EnforceClinicScope || entity.ClinicId == CurrentClinicId);
    }

    private void EnforceClinicWriteBoundary()
    {
        if (!EnforceClinicScope || CurrentClinicId is not Guid clinicId)
            return;

        foreach (var entry in ChangeTracker.Entries<ClinicEntity>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (entry.State == EntityState.Added && entry.Entity.ClinicId == Guid.Empty)
                entry.Entity.ClinicId = clinicId;

            if (entry.Entity.ClinicId != clinicId)
                throw new InvalidOperationException("Cross-clinic write access is not allowed.");

            if (entry.State == EntityState.Modified
                && entry.Property(x => x.ClinicId).IsModified
                && entry.Property(x => x.ClinicId).OriginalValue != clinicId)
            {
                throw new InvalidOperationException("ClinicId is immutable for clinic-owned data.");
            }
        }
    }
}
