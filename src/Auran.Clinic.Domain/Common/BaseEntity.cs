namespace Auran.Clinic.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public Guid? CreateByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }
}
