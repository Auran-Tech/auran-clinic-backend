namespace Auran.Clinic.Application.Auditing;

public sealed class AuditLogSearchRequest
{
    public Guid? ClinicId { get; set; }
    public Guid? UserId { get; set; }
    public string? Action { get; set; }
    public string? Category { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
