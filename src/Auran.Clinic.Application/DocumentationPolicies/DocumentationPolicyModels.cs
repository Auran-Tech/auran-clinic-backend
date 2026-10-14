namespace Auran.Clinic.Application.DocumentationPolicies;

public sealed class UpdateDocumentationPolicyRequest
{
    public required string[] RequiredFields { get; init; }
}

public sealed class AssignDoctorSpecialtyRequest
{
    public required string SpecialtyCode { get; init; }
}

public sealed record DocumentationPolicyResponse(
    string SpecialtyCode, IReadOnlyList<string> RequiredFields, int Version);

public interface IDocumentationPolicyService
{
    Task<DocumentationPolicyResponse?> GetAsync(string specialtyCode, CancellationToken ct = default);
    Task<DocumentationPolicyResponse> UpdateAsync(string specialtyCode, string[] fields, CancellationToken ct = default);
    Task<bool> AssignDoctorAsync(Guid doctorId, string specialtyCode, CancellationToken ct = default);
}
