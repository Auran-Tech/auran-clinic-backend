namespace Auran.Clinic.Domain.Entities;

public class ClinicalMeasurement : ClinicEntity
{
    public Guid PatientId { get; set; }
    public Guid? VisitId { get; set; }
    public Guid ClinicalFieldId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateOnly? DateValue { get; set; }
    public string? JsonValue { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public Guid RecordedByUserId { get; set; }
}
