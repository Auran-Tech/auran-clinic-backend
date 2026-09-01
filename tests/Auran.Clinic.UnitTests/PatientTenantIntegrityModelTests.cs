using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.UnitTests;

public sealed class PatientTenantIntegrityModelTests
{
    [Fact]
    public void Model_ContainsAllPatientCompositeTenantForeignKeys()
    {
        using var context = CreateContext();

        AssertTenantForeignKey<PatientCondition, Patient>(context, nameof(PatientCondition.PatientId));
        AssertTenantForeignKey<PatientCondition, User>(context, nameof(PatientCondition.RecordedByUserId));
        AssertTenantForeignKey<PatientAllergy, Patient>(context, nameof(PatientAllergy.PatientId));
        AssertTenantForeignKey<PatientAllergy, User>(context, nameof(PatientAllergy.RecordedByUserId));
        AssertTenantForeignKey<PatientMedication, Patient>(context, nameof(PatientMedication.PatientId));
        AssertTenantForeignKey<PatientMedication, User>(context, nameof(PatientMedication.RecordedByUserId));
        AssertTenantForeignKey<PatientProfileField, PatientProfileSection>(context, nameof(PatientProfileField.SectionId));
        AssertTenantForeignKey<PatientProfileFieldOption, PatientProfileField>(context, nameof(PatientProfileFieldOption.FieldId));
        AssertTenantForeignKey<PatientProfileValue, Patient>(context, nameof(PatientProfileValue.PatientId));
        AssertTenantForeignKey<PatientProfileValue, PatientProfileField>(context, nameof(PatientProfileValue.FieldId));
        AssertTenantForeignKey<PatientProfileValue, FileRecord>(context, nameof(PatientProfileValue.FileId));
        AssertTenantForeignKey<FileRecord, User>(context, nameof(FileRecord.UploadedByUserId));
        AssertTenantForeignKey<PatientAttachment, Patient>(context, nameof(PatientAttachment.PatientId));
        AssertTenantForeignKey<PatientAttachment, FileRecord>(context, nameof(PatientAttachment.FileId));
    }

    [Fact]
    public void Model_ContainsRequiredTenantCandidateKeys()
    {
        using var context = CreateContext();

        AssertTenantCandidateKey<Patient>(context);
        AssertTenantCandidateKey<PatientProfileSection>(context);
        AssertTenantCandidateKey<PatientProfileField>(context);
        AssertTenantCandidateKey<FileRecord>(context);
        AssertTenantCandidateKey<User>(context);
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
