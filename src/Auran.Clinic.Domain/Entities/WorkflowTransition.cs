namespace Auran.Clinic.Domain.Entities;

public class WorkflowTransition : ClinicEntity
{
    public Guid FromStatusId { get; set; }
    public Guid ToStatusId { get; set; }
}
