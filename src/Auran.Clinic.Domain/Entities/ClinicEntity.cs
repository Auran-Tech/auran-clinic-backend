using Auran.Clinic.Domain.Common;

namespace Auran.Clinic.Domain.Entities;

public abstract class ClinicEntity : BaseEntity
{
    public Guid ClinicId { get; set; }
}
