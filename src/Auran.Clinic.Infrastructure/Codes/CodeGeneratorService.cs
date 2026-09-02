using System.Data;
using System.Data.Common;
using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Codes;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Auran.Clinic.Infrastructure.Codes;

public sealed class CodeGeneratorService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext) : ICodeGeneratorService
{
    public async Task<string> GenerateAsync(
        CodeScope scope,
        Guid? clinicId,
        CodeType codeType,
        string prefix,
        CancellationToken cancellationToken = default)
    {
        var normalizedPrefix = NormalizePrefix(prefix);
        ValidateScope(scope, clinicId);

        if (scope == CodeScope.Clinic && currentUserContext.IsAuthenticated && currentUserContext.ClinicId != clinicId)
            throw new InvalidOperationException("Cross-clinic code generation is not allowed.");

        var now = DateTime.UtcNow;
        var year = now.Year;
        var actorId = currentUserContext.UserId;
        var lockResource = BuildLockResource(scope, clinicId, codeType, normalizedPrefix, year);
        var ownsTransaction = dbContext.Database.CurrentTransaction is null;
        IDbContextTransaction? transaction = null;

        try
        {
            if (ownsTransaction)
                transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            var currentTransaction = dbContext.Database.CurrentTransaction
                ?? throw new InvalidOperationException("A database transaction is required to generate a business code.");

            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.Transaction = currentTransaction.GetDbTransaction();
            command.CommandText = """
                SET NOCOUNT ON;

                DECLARE @LockResult int;
                EXEC @LockResult = sys.sp_getapplock
                    @Resource = @LockResource,
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Transaction',
                    @LockTimeout = 15000;

                IF @LockResult < 0
                    THROW 51000, 'Could not acquire the business code counter lock.', 1;

                DECLARE @NextNumber TABLE ([Value] int NOT NULL);

                UPDATE [CodeCounters] WITH (UPDLOCK)
                SET [LastNumber] = [LastNumber] + 1,
                    [UpdatedDate] = @Now,
                    [UpdatedByUserId] = @ActorId
                OUTPUT inserted.[LastNumber] INTO @NextNumber([Value])
                WHERE [Scope] = @Scope
                  AND (([ClinicId] = @ClinicId) OR ([ClinicId] IS NULL AND @ClinicId IS NULL))
                  AND [CodeType] = @CodeType
                  AND [Prefix] = @Prefix
                  AND [Year] = @Year;

                IF NOT EXISTS (SELECT 1 FROM @NextNumber)
                BEGIN
                    INSERT INTO [CodeCounters]
                    (
                        [Id], [Scope], [ClinicId], [CodeType], [Prefix], [Year], [LastNumber],
                        [CreatedDate], [UpdatedDate], [CreateByUserId], [UpdatedByUserId]
                    )
                    OUTPUT inserted.[LastNumber] INTO @NextNumber([Value])
                    VALUES
                    (
                        @Id, @Scope, @ClinicId, @CodeType, @Prefix, @Year, 1,
                        @Now, NULL, @ActorId, NULL
                    );
                END

                SELECT TOP (1) [Value] FROM @NextNumber;
                """;

            AddParameter(command, "@LockResource", lockResource);
            AddParameter(command, "@Id", Guid.NewGuid());
            AddParameter(command, "@Scope", scope.ToString());
            AddParameter(command, "@ClinicId", clinicId);
            AddParameter(command, "@CodeType", codeType.ToString());
            AddParameter(command, "@Prefix", normalizedPrefix);
            AddParameter(command, "@Year", year);
            AddParameter(command, "@Now", now);
            AddParameter(command, "@ActorId", actorId);

            var scalar = await command.ExecuteScalarAsync(cancellationToken);
            var nextNumber = scalar is null or DBNull
                ? throw new InvalidOperationException("The next business code number could not be generated.")
                : Convert.ToInt32(scalar);

            if (ownsTransaction && transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return $"{normalizedPrefix}-{year}-{nextNumber}";
        }
        catch
        {
            if (ownsTransaction && transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    private static string BuildLockResource(
        CodeScope scope,
        Guid? clinicId,
        CodeType codeType,
        string prefix,
        int year) =>
        $"Auran.CodeCounter:{scope}:{clinicId?.ToString("N") ?? "Platform"}:{codeType}:{prefix}:{year}";

    private static void ValidateScope(CodeScope scope, Guid? clinicId)
    {
        if (scope == CodeScope.Platform && clinicId.HasValue)
            throw new ArgumentException("Platform code counters cannot have a clinic id.", nameof(clinicId));
        if (scope == CodeScope.Clinic && (!clinicId.HasValue || clinicId == Guid.Empty))
            throw new ArgumentException("Clinic code counters require a clinic id.", nameof(clinicId));
    }

    private static string NormalizePrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("A code prefix is required.", nameof(prefix));

        var normalized = prefix.Trim().ToUpperInvariant();
        if (normalized.Length > 20 || normalized.Any(c => !char.IsLetterOrDigit(c) && c is not '_' and not '-'))
            throw new ArgumentException("Code prefix must contain only letters, numbers, underscores or hyphens and be at most 20 characters.", nameof(prefix));
        return normalized;
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
