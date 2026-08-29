using System.ComponentModel.DataAnnotations;
using Auran.Clinic.Api.Validation;
using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Api.Contracts.Auditing;

[DateRange(nameof(FromUtc), nameof(ToUtc), ErrorMessage = "FromUtc must be earlier than or equal to ToUtc.")]
public sealed class AuditLogSearchApiRequest
{
    public AuditScope? Scope { get; set; }
    public Guid? ClinicId { get; set; }
    public ActorType? ActorType { get; set; }
    public Guid? ActorId { get; set; }

    [StringLength(160)]
    public string? Action { get; set; }

    [StringLength(100)]
    public string? Category { get; set; }

    [StringLength(160)]
    public string? EntityType { get; set; }

    [StringLength(100)]
    public string? EntityId { get; set; }

    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 200)]
    public int PageSize { get; set; } = 50;
}
