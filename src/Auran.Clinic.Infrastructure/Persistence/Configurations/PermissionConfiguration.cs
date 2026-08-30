using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasIndex(x => x.Key).IsUnique();
        builder.Property(x => x.Key).HasMaxLength(160).IsRequired();
        builder.Property(x => x.GroupKey).HasMaxLength(100).IsRequired();

        builder.HasData(Permissions.All.Select(definition => new Permission
        {
            Id = PermissionSeedIds.Permission(definition.Key),
            Key = definition.Key,
            GroupKey = definition.GroupKey,
            CreatedDate = SeedDate
        }));
    }
}
