using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Domain.Entities;

public class Visit : ClinicEntity
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public VisitStatus Status { get; set; } = VisitStatus.Open;
    public DocumentationStatus DocumentationStatus { get; set; } = DocumentationStatus.NotStarted;
    public DateTime EntryAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? ExitAtUtc { get; set; }
    public string? ChiefComplaint { get; set; }
    public string? Examination { get; set; }
    public string? Diagnosis { get; set; }
    public string? Notes { get; set; }
    public string? TreatmentPlan { get; set; }
    public string? FollowUpText { get; set; }
}

public class VisitSession : ClinicEntity
{
    public Guid VisitId { get; set; }
    public Guid DoctorId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
}

public class ClinicalOrderSectionDefinition : ClinicEntity
{
    public required string Name { get; set; }
    public ClinicalOrderSectionType SectionType { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class ClinicalOrder : ClinicEntity
{
    public Guid VisitId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid CreatedByUserId { get; set; }
}

public class ClinicalOrderSection : ClinicEntity
{
    public Guid ClinicalOrderId { get; set; }
    public Guid SectionDefinitionId { get; set; }
    public int SortOrder { get; set; }
    public string? TextValue { get; set; }
}

public class ClinicalOrderItem : ClinicEntity
{
    public Guid ClinicalOrderSectionId { get; set; }
    public required string Name { get; set; }
    public string? DetailsJson { get; set; }
}
