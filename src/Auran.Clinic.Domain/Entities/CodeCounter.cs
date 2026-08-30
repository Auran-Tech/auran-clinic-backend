namespace Auran.Clinic.Domain.Entities;

public class CodeCounter : ClinicEntity
{
    public required string CodeType { get; set; }

    public required string ScopeKey { get; set; }

    public long LastNumber { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
