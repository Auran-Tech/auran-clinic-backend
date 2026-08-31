namespace Auran.Clinic.IntegrationTests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlIntegrationFactAttribute : FactAttribute
{
    public SqlIntegrationFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("AURAN_SQL_INTEGRATION"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Requires the SQL Server integration-test environment.";
        }
    }
}
