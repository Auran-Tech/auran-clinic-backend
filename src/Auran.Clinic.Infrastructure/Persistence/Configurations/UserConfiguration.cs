using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(user => user.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(user => user.IdentityAccountType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(AccountType.Clinic)
            .IsRequired();

        builder.HasOne<ApplicationIdentityUser>()
            .WithOne()
            .HasForeignKey<User>(user => new { user.IdentityUserId, user.IdentityAccountType })
            .HasPrincipalKey<ApplicationIdentityUser>(identity => new { identity.Id, identity.AccountType })
            .OnDelete(DeleteBehavior.Restrict);

        // Candidate key used by tenant-safe composite foreign keys. Id remains the primary key.
        builder.HasAlternateKey(user => new { user.Id, user.ClinicId });

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_Users_IdentityAccountType",
            "[IdentityAccountType] = 'Clinic'"));
    }
}
