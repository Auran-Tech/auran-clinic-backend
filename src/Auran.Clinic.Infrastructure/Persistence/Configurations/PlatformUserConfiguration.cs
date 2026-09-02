using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class PlatformUserConfiguration : IEntityTypeConfiguration<PlatformUser>
{
    public void Configure(EntityTypeBuilder<PlatformUser> builder)
    {
        builder.Property(x => x.IdentityUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(64);
        builder.HasIndex(x => x.IdentityUserId).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasOne<ApplicationIdentityUser>()
            .WithOne()
            .HasForeignKey<PlatformUser>(x => x.IdentityUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
