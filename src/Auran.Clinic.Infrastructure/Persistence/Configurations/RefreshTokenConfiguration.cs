using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.ClinicId, x.UserId, x.ExpiresDate });
        builder.Ignore(x => x.IsActive);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => new { token.UserId, token.ClinicId })
            .HasPrincipalKey(user => new { user.Id, user.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
