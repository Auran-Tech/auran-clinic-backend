using Auran.Clinic.Domain.Entities;
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

        // Candidate key used by tenant-safe composite foreign keys. Id remains the primary key.
        builder.HasAlternateKey(user => new { user.Id, user.ClinicId });
    }
}
