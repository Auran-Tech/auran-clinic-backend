using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Application.Codes;

public interface ICodeGeneratorService
{
    Task<string> GenerateAsync(
        CodeScope scope,
        Guid? clinicId,
        CodeType codeType,
        string prefix,
        CancellationToken cancellationToken = default);
}
