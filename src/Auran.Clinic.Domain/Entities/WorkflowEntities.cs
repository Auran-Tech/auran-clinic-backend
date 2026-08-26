namespace Auran.Clinic.Domain.Entities;

public class WorkflowStatus : ClinicEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Color { get; set; }
    public int SortOrder { get; set; }
    public bool IsSystemFinal { get; set; }
}

public class WorkflowTransition : ClinicEntity
{
    public Guid FromStatusId { get; set; }
    public Guid ToStatusId { get; set; }
}

public class QueueEntry : ClinicEntity
{
    public Guid PatientId { get; set; }
    public Guid VisitId { get; set; }
    public Guid? DoctorId { get; set; }
    public Guid WorkflowStatusId { get; set; }
    public DateTime EntryAtUtc { get; set; }
    public DateTime? ExitAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public class QueueStatusHistory : ClinicEntity
{
    public Guid QueueEntryId { get; set; }
    public Guid? FromStatusId { get; set; }
    public Guid ToStatusId { get; set; }
    public DateTime ChangedAtUtc { get; set; }
    public Guid ChangedByUserId { get; set; }
    public string? Notes { get; set; }
}
