namespace Auran.Clinic.Application.ClinicalOrderAttachments;

public interface IClinicalOrderAttachmentService
{
    Task<ClinicalOrderAttachmentWorkspaceResponse?> GetWorkspaceAsync(
        Guid visitId,
        CancellationToken cancellationToken = default);

    Task<ClinicalOrderAttachmentResult> LinkAsync(
        LinkClinicalOrderAttachmentRequest request,
        CancellationToken cancellationToken = default);

    Task<ClinicalOrderAttachmentResult> DeleteAsync(
        DeleteClinicalOrderAttachmentRequest request,
        CancellationToken cancellationToken = default);
}
