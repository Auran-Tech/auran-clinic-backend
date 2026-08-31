using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasIndex(x => x.Key).IsUnique();
        builder.Property(x => x.Key).HasMaxLength(160).IsRequired();
        builder.Property(x => x.GroupKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Scope)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.HasIndex(x => x.Scope);
    }
}
