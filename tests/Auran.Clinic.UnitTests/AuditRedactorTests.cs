using Auran.Clinic.Infrastructure.Auditing;

namespace Auran.Clinic.UnitTests;

public sealed class AuditRedactorTests
{
    [Fact]
    public void Redact_ReplacesSensitiveValuesAndPreservesSafeMetadata()
    {
        var metadata = new Dictionary<string, object?>
        {
            ["refreshToken"] = "sensitive",
            ["Authorization"] = "Bearer sensitive",
            ["entityName"] = "Clinic settings"
        };

        var result = AuditRedactor.Redact(metadata);

        Assert.Equal("[REDACTED]", result["refreshToken"]);
        Assert.Equal("[REDACTED]", result["Authorization"]);
        Assert.Equal("Clinic settings", result["entityName"]);
    }
}
