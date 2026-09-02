using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class PlatformRefreshTokenConfiguration : IEntityTypeConfiguration<PlatformRefreshToken>
{
    public void Configure(EntityTypeBuilder<PlatformRefreshToken> builder)
    {
        builder.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(token => token.ReplacedByTokenHash).HasMaxLength(128);
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => new { token.PlatformUserId, token.ExpiresDate });
        builder.Ignore(token => token.IsActive);

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(token => token.PlatformUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
