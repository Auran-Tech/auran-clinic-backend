using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.UnitTests;

public sealed class WorkflowVisitTenantIntegrityModelTests
{
    [Fact]
    public void Model_ContainsAllWorkflowVisitCompositeTenantForeignKeys()
    {
        using var context = CreateContext();

        AssertTenantForeignKey<WorkflowTransition, WorkflowStatus>(context, nameof(WorkflowTransition.FromStatusId));
        AssertTenantForeignKey<WorkflowTransition, WorkflowStatus>(context, nameof(WorkflowTransition.ToStatusId));

        AssertTenantForeignKey<QueueEntry, Patient>(context, nameof(QueueEntry.PatientId));
        AssertTenantForeignKey<QueueEntry, Visit>(context, nameof(QueueEntry.VisitId));
        AssertTenantForeignKey<QueueEntry, User>(context, nameof(QueueEntry.DoctorId));
        AssertTenantForeignKey<QueueEntry, WorkflowStatus>(context, nameof(QueueEntry.WorkflowStatusId));

        AssertTenantForeignKey<QueueStatusHistory, QueueEntry>(context, nameof(QueueStatusHistory.QueueEntryId));
        AssertTenantForeignKey<QueueStatusHistory, WorkflowStatus>(context, nameof(QueueStatusHistory.FromStatusId));
        AssertTenantForeignKey<QueueStatusHistory, WorkflowStatus>(context, nameof(QueueStatusHistory.ToStatusId));
        AssertTenantForeignKey<QueueStatusHistory, User>(context, nameof(QueueStatusHistory.ChangedByUserId));

        AssertTenantForeignKey<Visit, Patient>(context, nameof(Visit.PatientId));
        AssertTenantForeignKey<Visit, User>(context, nameof(Visit.DoctorId));

        AssertTenantForeignKey<VisitSession, Visit>(context, nameof(VisitSession.VisitId));
        AssertTenantForeignKey<VisitSession, User>(context, nameof(VisitSession.DoctorId));
        AssertTenantForeignKey<VisitSession, User>(context, nameof(VisitSession.CreatedByUserId));

        AssertTenantForeignKey<FollowUp, Patient>(context, nameof(FollowUp.PatientId));
        AssertTenantForeignKey<FollowUp, Visit>(context, nameof(FollowUp.VisitId));
        AssertTenantForeignKey<FollowUp, User>(context, nameof(FollowUp.DoctorId));

        AssertTenantForeignKey<ClinicalFieldOption, ClinicalField>(context, nameof(ClinicalFieldOption.ClinicalFieldId));

        AssertTenantForeignKey<ClinicalMeasurement, Patient>(context, nameof(ClinicalMeasurement.PatientId));
        AssertTenantForeignKey<ClinicalMeasurement, Visit>(context, nameof(ClinicalMeasurement.VisitId));
        AssertTenantForeignKey<ClinicalMeasurement, ClinicalField>(context, nameof(ClinicalMeasurement.ClinicalFieldId));
        AssertTenantForeignKey<ClinicalMeasurement, User>(context, nameof(ClinicalMeasurement.RecordedByUserId));

        AssertTenantForeignKey<ClinicalOrder, Visit>(context, nameof(ClinicalOrder.VisitId));
        AssertTenantForeignKey<ClinicalOrder, Patient>(context, nameof(ClinicalOrder.PatientId));
        AssertTenantForeignKey<ClinicalOrder, User>(context, nameof(ClinicalOrder.DoctorId));
        AssertTenantForeignKey<ClinicalOrder, User>(context, nameof(ClinicalOrder.CreatedByUserId));

        AssertTenantForeignKey<ClinicalOrderSection, ClinicalOrder>(context, nameof(ClinicalOrderSection.ClinicalOrderId));
        AssertTenantForeignKey<ClinicalOrderSection, ClinicalOrderSectionDefinition>(context, nameof(ClinicalOrderSection.SectionDefinitionId));
        AssertTenantForeignKey<ClinicalOrderItem, ClinicalOrderSection>(context, nameof(ClinicalOrderItem.ClinicalOrderSectionId));
        AssertTenantForeignKey<ClinicalOrderAttachment, ClinicalOrder>(context, nameof(ClinicalOrderAttachment.ClinicalOrderId));
        AssertTenantForeignKey<ClinicalOrderAttachment, ClinicalOrderSection>(context, nameof(ClinicalOrderAttachment.ClinicalOrderSectionId));
        AssertTenantForeignKey<ClinicalOrderAttachment, FileRecord>(context, nameof(ClinicalOrderAttachment.FileId));
    }

    [Fact]
    public void Model_ContainsRequiredWorkflowVisitTenantCandidateKeys()
    {
        using var context = CreateContext();

        AssertTenantCandidateKey<WorkflowStatus>(context);
        AssertTenantCandidateKey<QueueEntry>(context);
        AssertTenantCandidateKey<Visit>(context);
        AssertTenantCandidateKey<ClinicalField>(context);
        AssertTenantCandidateKey<ClinicalOrder>(context);
        AssertTenantCandidateKey<ClinicalOrderSectionDefinition>(context);
        AssertTenantCandidateKey<ClinicalOrderSection>(context);
    }

    private static void AssertTenantForeignKey<TDependent, TPrincipal>(
        AuranClinicDbContext context,
        string relationshipProperty)
        where TDependent : class
        where TPrincipal : class
    {
        var dependentType = context.Model.FindEntityType(typeof(TDependent));
        Assert.NotNull(dependentType);

        var expectedProperties = new[] { relationshipProperty, nameof(ClinicEntity.ClinicId) };
        var foreignKey = dependentType.GetForeignKeys().SingleOrDefault(candidate =>
            candidate.PrincipalEntityType.ClrType == typeof(TPrincipal) &&
            candidate.Properties.Select(property => property.Name).SequenceEqual(expectedProperties));

        Assert.NotNull(foreignKey);
        Assert.Equal(
            new[] { "Id", nameof(ClinicEntity.ClinicId) },
            foreignKey.PrincipalKey.Properties.Select(property => property.Name));
    }

    private static void AssertTenantCandidateKey<TEntity>(AuranClinicDbContext context)
        where TEntity : class
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity));
        Assert.NotNull(entityType);

        Assert.Contains(
            entityType.GetKeys(),
            key => key.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { "Id", nameof(ClinicEntity.ClinicId) }));
    }

    private static AuranClinicDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AuranClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AuranClinicDbContext(options, new TestCurrentUserContext());
    }

    private sealed class TestCurrentUserContext : ICurrentUserContext
    {
        public bool IsAuthenticated => false;
        public Guid? UserId => null;
        public Guid? ClinicId => null;
        public bool IsSuperUser => false;
    }
}
