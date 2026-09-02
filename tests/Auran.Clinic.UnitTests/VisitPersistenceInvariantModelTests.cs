using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Auran.Clinic.UnitTests;

public sealed class VisitPersistenceInvariantModelTests
{
    [Fact]
    public void Model_EnforcesQueueAndActiveSessionUniqueness()
    {
        using var context = CreateContext();

        var queueType = context.Model.FindEntityType(typeof(QueueEntry));
        Assert.NotNull(queueType);
        var queueIndex = queueType.GetIndexes().SingleOrDefault(index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(ClinicEntity.ClinicId), nameof(QueueEntry.VisitId) }));
        Assert.NotNull(queueIndex);
        Assert.True(queueIndex.IsUnique);

        var sessionType = context.Model.FindEntityType(typeof(VisitSession));
        Assert.NotNull(sessionType);
        var activeSessionIndex = sessionType.GetIndexes().SingleOrDefault(index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(ClinicEntity.ClinicId), nameof(VisitSession.VisitId) }));
        Assert.NotNull(activeSessionIndex);
        Assert.True(activeSessionIndex.IsUnique);
        Assert.Equal("[EndedAtUtc] IS NULL", activeSessionIndex.GetFilter());
    }

    [Fact]
    public void Model_UsesVisitRowVersionAndNoLongerStoresFollowUpText()
    {
        using var context = CreateContext();
        var visitType = context.Model.FindEntityType(typeof(Visit));
        Assert.NotNull(visitType);

        var rowVersion = visitType.FindProperty(nameof(Visit.RowVersion));
        Assert.NotNull(rowVersion);
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);

        Assert.Null(visitType.FindProperty("FollowUpText"));
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
