namespace Auran.Clinic.Application.Abstractions;

public interface ICodeGenerator
{
    Task<long> GetNextNumberAsync(
        string codeType,
        string scopeKey,
        CancellationToken cancellationToken = default);
}
