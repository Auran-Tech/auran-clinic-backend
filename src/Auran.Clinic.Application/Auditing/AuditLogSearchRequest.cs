using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Application.Auditing;

public sealed class AuditLogSearchRequest
{
    public AuditScope? Scope { get; set; }
    public Guid? ClinicId { get; set; }
    public ActorType? ActorType { get; set; }
    public Guid? ActorId { get; set; }
    public string? Action { get; set; }
    public string? Category { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
