using System.Data;
using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Persistence;

public sealed class CodeGenerator(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext) : ICodeGenerator
{
    public async Task<long> GetNextNumberAsync(
        string codeType,
        string scopeKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codeType))
            throw new ArgumentException("Code type is required.", nameof(codeType));
        if (string.IsNullOrWhiteSpace(scopeKey))
            throw new ArgumentException("Scope key is required.", nameof(scopeKey));
        if (!currentUserContext.IsAuthenticated || currentUserContext.ClinicId is not Guid clinicId)
            throw new InvalidOperationException("An authenticated clinic context is required to generate codes.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var normalizedType = codeType.Trim();
        var normalizedScope = scopeKey.Trim();
        var counter = await dbContext.CodeCounters.SingleOrDefaultAsync(
            x => x.ClinicId == clinicId &&
                 x.CodeType == normalizedType &&
                 x.ScopeKey == normalizedScope,
            cancellationToken);

        if (counter is null)
        {
            counter = new CodeCounter
            {
                ClinicId = clinicId,
                CodeType = normalizedType,
                ScopeKey = normalizedScope,
                LastNumber = 1
            };
            dbContext.CodeCounters.Add(counter);
        }
        else
        {
            counter.LastNumber++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return counter.LastNumber;
    }
}
