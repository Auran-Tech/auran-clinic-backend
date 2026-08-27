using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class PlatformRoleConfiguration : IEntityTypeConfiguration<PlatformRole>
{
    public void Configure(EntityTypeBuilder<PlatformRole> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(128);
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
