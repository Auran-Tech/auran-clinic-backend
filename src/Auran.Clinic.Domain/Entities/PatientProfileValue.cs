namespace Auran.Clinic.Domain.Entities;

public class PatientProfileValue : ClinicEntity
{
    public Guid PatientId { get; set; }
    public Guid FieldId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateOnly? DateValue { get; set; }
    public Guid? FileId { get; set; }
    public string? JsonValue { get; set; }
}
