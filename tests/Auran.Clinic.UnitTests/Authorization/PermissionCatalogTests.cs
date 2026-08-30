using Auran.Clinic.Application.Authorization;

namespace Auran.Clinic.UnitTests.Authorization;

public sealed class PermissionCatalogTests
{
    [Fact]
    public void PermissionKeys_ShouldBeUniqueAndStable()
    {
        var definitions = Permissions.All;

        Assert.NotEmpty(definitions);
        Assert.Equal(definitions.Count, definitions.Select(x => x.Key).Distinct(StringComparer.Ordinal).Count());
        Assert.All(definitions, permission =>
        {
            Assert.DoesNotContain('.', permission.Key);
            Assert.DoesNotContain(' ', permission.Key);
            Assert.False(string.IsNullOrWhiteSpace(permission.GroupKey));
            Assert.False(string.IsNullOrWhiteSpace(permission.EnglishDescription));
            Assert.False(string.IsNullOrWhiteSpace(permission.ArabicDescription));
        });
    }

    [Fact]
    public void PermissionCatalog_ShouldCoverApprovedPrototypeCapabilities()
    {
        var keys = Permissions.All.Select(x => x.Key).ToHashSet(StringComparer.Ordinal);

        var expected = new[]
        {
            Permissions.DashboardView,
            Permissions.PatientView,
            Permissions.PatientCreate,
            Permissions.PatientEditBasic,
            Permissions.MedicalProfileView,
            Permissions.MedicalProfileEdit,
            Permissions.MeasurementCreate,
            Permissions.QueueView,
            Permissions.QueueCheckIn,
            Permissions.QueueMove,
            Permissions.QueueExit,
            Permissions.VisitView,
            Permissions.VisitStart,
            Permissions.VisitEdit,
            Permissions.VisitSession,
            Permissions.PrescriptionCreate,
            Permissions.DocumentationComplete,
            Permissions.FollowUpView,
            Permissions.ReportsView,
            Permissions.UsersManage,
            Permissions.UsersManageStatus,
            Permissions.RbacView,
            Permissions.ConfigManage,
            Permissions.AuditView,
            Permissions.SettingsManage
        };

        Assert.All(expected, key => Assert.Contains(key, keys));
    }
}
