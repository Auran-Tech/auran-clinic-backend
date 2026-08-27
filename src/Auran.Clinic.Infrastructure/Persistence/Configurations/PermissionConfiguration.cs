using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Code).HasMaxLength(160);
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.Group).HasMaxLength(100);
        builder.Property(x => x.Scope)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.HasIndex(x => x.Scope);
    }
}
