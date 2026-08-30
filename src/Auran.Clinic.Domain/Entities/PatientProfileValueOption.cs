namespace Auran.Clinic.Domain.Entities;

public class PatientProfileValueOption : ClinicEntity
{
    public Guid PatientProfileValueId { get; set; }
    public Guid OptionId { get; set; }
}
