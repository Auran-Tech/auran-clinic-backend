using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class PlatformRefreshTokenConfiguration : IEntityTypeConfiguration<PlatformRefreshToken>
{
    public void Configure(EntityTypeBuilder<PlatformRefreshToken> builder)
    {
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ReplacedByTokenHash).HasMaxLength(128);
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.PlatformUserId, x.ExpiresDate });
        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(x => x.PlatformUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
