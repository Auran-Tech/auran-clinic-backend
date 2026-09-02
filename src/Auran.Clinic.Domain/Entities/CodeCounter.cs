using Auran.Clinic.Domain.Common;
using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Domain.Entities;

public class CodeCounter : BaseEntity
{
    public CodeScope Scope { get; set; }
    public Guid? ClinicId { get; set; }
    public CodeType CodeType { get; set; }
    public required string Prefix { get; set; }
    public int Year { get; set; }
    public int LastNumber { get; set; }
}
