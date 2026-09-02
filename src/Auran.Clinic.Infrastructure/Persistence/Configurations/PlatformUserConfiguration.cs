using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class PlatformUserConfiguration : IEntityTypeConfiguration<PlatformUser>
{
    public void Configure(EntityTypeBuilder<PlatformUser> builder)
    {
        builder.Property(user => user.IdentityUserId).HasMaxLength(450).IsRequired();
        builder.Property(user => user.IdentityAccountType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(AccountType.Platform)
            .IsRequired();
        builder.Property(user => user.FullName).HasMaxLength(200).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(256).IsRequired();
        builder.Property(user => user.Phone).HasMaxLength(64);
        builder.Property(user => user.IsActive).HasDefaultValue(true).IsRequired();

        builder.HasIndex(user => user.Email).IsUnique();

        builder.HasOne<ApplicationIdentityUser>()
            .WithOne()
            .HasForeignKey<PlatformUser>(user => new { user.IdentityUserId, user.IdentityAccountType })
            .HasPrincipalKey<ApplicationIdentityUser>(identity => new { identity.Id, identity.AccountType })
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_PlatformUsers_IdentityAccountType",
            "[IdentityAccountType] = 'Platform'"));
    }
}
