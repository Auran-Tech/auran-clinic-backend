using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.UnitTests;

public sealed class IdentityPersistenceModelTests
{
    [Fact]
    public void Model_UsesIdentityUsersWithoutIdentityRoleSchema()
    {
        using var context = CreateContext();

        Assert.NotNull(context.Model.FindEntityType(typeof(ApplicationIdentityUser)));
        Assert.Null(context.Model.FindEntityType(typeof(IdentityRole)));
        Assert.Null(context.Model.FindEntityType(typeof(IdentityRoleClaim<string>)));
        Assert.Null(context.Model.FindEntityType(typeof(IdentityUserRole<string>)));

        Assert.NotNull(context.Model.FindEntityType(typeof(Role)));
        Assert.NotNull(context.Model.FindEntityType(typeof(UserRole)));
        Assert.NotNull(context.Model.FindEntityType(typeof(RolePermission)));
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
