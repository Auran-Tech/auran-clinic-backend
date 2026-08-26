namespace Auran.Clinic.Domain.Entities;

public class WorkflowStatus : ClinicEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Color { get; set; }
    public int SortOrder { get; set; }
    public bool IsSystemFinal { get; set; }
}
