using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(x => x.Scope).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ActorType).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ActorIdentityUserId).HasMaxLength(450);
        builder.Property(x => x.ActorDisplayName).HasMaxLength(200);
        builder.Property(x => x.ActorEmail).HasMaxLength(256);
        builder.Property(x => x.Action).HasMaxLength(160);
        builder.Property(x => x.Category).HasMaxLength(100);
        builder.Property(x => x.EntityType).HasMaxLength(160);
        builder.Property(x => x.EntityId).HasMaxLength(100);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(1000);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);

        builder.HasIndex(x => new { x.Scope, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.ClinicId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.ActorType, x.ActorId });
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}
