using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.UnitTests;

public sealed class AuditTenantIntegrityModelTests
{
    [Fact]
    public void Model_UsesCompositeTenantForeignKeyForAuditActor()
    {
        using var context = CreateContext();
        var auditType = context.Model.FindEntityType(typeof(AuditLog));
        Assert.NotNull(auditType);

        var foreignKey = auditType.GetForeignKeys().SingleOrDefault(candidate =>
            candidate.PrincipalEntityType.ClrType == typeof(User) &&
            candidate.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(AuditLog.ActorUserId), nameof(ClinicEntity.ClinicId) }));

        Assert.NotNull(foreignKey);
        Assert.Equal(
            new[] { "Id", nameof(ClinicEntity.ClinicId) },
            foreignKey.PrincipalKey.Properties.Select(property => property.Name));

        Assert.DoesNotContain(
            auditType.GetForeignKeys(),
            candidate =>
                candidate.PrincipalEntityType.ClrType == typeof(User) &&
                candidate.Properties.Select(property => property.Name)
                    .SequenceEqual(new[] { nameof(AuditLog.ActorUserId) }));
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
