using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class ApplicationIdentityUserConfiguration : IEntityTypeConfiguration<ApplicationIdentityUser>
{
    public void Configure(EntityTypeBuilder<ApplicationIdentityUser> builder)
    {
        builder.Property(x => x.AccountType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(AccountType.Clinic);
    }
}
