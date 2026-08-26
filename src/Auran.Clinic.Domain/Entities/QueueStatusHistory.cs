namespace Auran.Clinic.Domain.Entities;

public class QueueStatusHistory : ClinicEntity
{
    public Guid QueueEntryId { get; set; }
    public Guid? FromStatusId { get; set; }
    public Guid ToStatusId { get; set; }
    public DateTime ChangedAtUtc { get; set; }
    public Guid ChangedByUserId { get; set; }
    public string? Notes { get; set; }
}
