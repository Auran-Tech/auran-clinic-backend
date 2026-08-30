using Auran.Clinic.Domain.Common;

namespace Auran.Clinic.Domain.Entities;

public class PermissionTranslation : BaseEntity
{
    public Guid PermissionId { get; set; }

    public required string LanguageCode { get; set; }

    public required string Description { get; set; }
}
