using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class ApplicationIdentityUserConfiguration : IEntityTypeConfiguration<ApplicationIdentityUser>
{
    public void Configure(EntityTypeBuilder<ApplicationIdentityUser> builder)
    {
        builder.Property(identity => identity.AccountType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(AccountType.Clinic)
            .IsRequired();

        builder.HasAlternateKey(identity => new { identity.Id, identity.AccountType });
    }
}
