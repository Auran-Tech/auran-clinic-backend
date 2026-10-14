using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Auran.Clinic.IntegrationTests.ReleaseReadiness;

/// <summary>
/// Executed only against the disposable SQL Server CI database.
/// A successful BACKUP without a separate RESTORE is not recovery verification.
/// </summary>
public sealed class SqlBackupRestoreTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Disposable_SQL_Server_backup_can_be_restored_and_checked()
    {
        // Fail closed if CI explicitly opted out or the target is not its known DB.
        if (Environment.GetEnvironmentVariable("AURAN_CI_BACKUP_RESTORE_TEST") != "1")
            return;

        Assert.Equal("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var rawConnection = configuration.GetConnectionString("DefaultConnection");
        Assert.False(string.IsNullOrWhiteSpace(rawConnection));
        var source = new SqlConnectionStringBuilder(rawConnection);
        Assert.Equal("AuranClinicCi", source.InitialCatalog);

        var target = "AuranClinicRestoreSmoke_" + Guid.NewGuid().ToString("N")[..12];
        var backupPath = "/var/opt/mssql/data/" + target + ".bak";
        var masterSettings = new SqlConnectionStringBuilder(rawConnection)
        {
            InitialCatalog = "master"
        };

        await using var connection = new SqlConnection(masterSettings.ConnectionString);
        await connection.OpenAsync();

        static async Task ExecuteAsync(SqlConnection connection, string sql)
        {
            await using var command = new SqlCommand(sql, connection)
            {
                CommandTimeout = 180
            };
            await command.ExecuteNonQueryAsync();
        }

        static string Esc(string input) => input.Replace("'", "''", StringComparison.Ordinal);
        static string Quoted(string input) => "[" + input.Replace("]", "]]", StringComparison.Ordinal) + "]";

        var sourceDb = Quoted(source.InitialCatalog);
        var targetDb = Quoted(target);

        try
        {
            await ExecuteAsync(connection,
                $"BACKUP DATABASE {sourceDb} TO DISK = N'{Esc(backupPath)}' WITH COPY_ONLY, INIT, CHECKSUM;");
            await ExecuteAsync(connection,
                $"RESTORE VERIFYONLY FROM DISK = N'{Esc(backupPath)}' WITH CHECKSUM;");

            var logicalData = "";
            var logicalLog = "";
            await using (var command = new SqlCommand(
                $"RESTORE FILELISTONLY FROM DISK = N'{Esc(backupPath)}';", connection))
            {
                command.CommandTimeout = 180;
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var name = reader.GetString(reader.GetOrdinal("LogicalName"));
                    var type = reader.GetString(reader.GetOrdinal("Type"));
                    if (type == "D") logicalData = name;
                    if (type == "L") logicalLog = name;
                }
            }

            Assert.NotEmpty(logicalData);
            Assert.NotEmpty(logicalLog);

            await ExecuteAsync(connection,
                $"RESTORE DATABASE {targetDb} FROM DISK = N'{Esc(backupPath)}' " +
                $"WITH MOVE N'{Esc(logicalData)}' TO N'/var/opt/mssql/data/{target}.mdf', " +
                $"MOVE N'{Esc(logicalLog)}' TO N'/var/opt/mssql/data/{target}.ldf', RECOVERY;");

            await ExecuteAsync(connection, $"DBCC CHECKDB ({targetDb}) WITH NO_INFOMSGS;");

            await using var validation = new SqlCommand(
                $"SELECT COUNT(*) FROM {targetDb}.dbo.__EFMigrationsHistory;", connection);
            Assert.True((int)(await validation.ExecuteScalarAsync())! > 0);
        }
        finally
        {
            await ExecuteAsync(connection,
                $"IF DB_ID(N'{Esc(target)}') IS NOT NULL BEGIN " +
                $"ALTER DATABASE {targetDb} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                $"DROP DATABASE {targetDb}; END;");
            // The SQL Server service container is disposable at the end of CI;
            // backup files are intentionally not managed using unsafe xp_cmdshell.
        }
    }
}
