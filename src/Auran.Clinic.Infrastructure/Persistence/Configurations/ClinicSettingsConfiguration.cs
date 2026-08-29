using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public class ClinicSettingsConfiguration : IEntityTypeConfiguration<ClinicSettings>
{
    public void Configure(EntityTypeBuilder<ClinicSettings> builder)
    {
        builder.HasIndex(x => x.ClinicId).IsUnique();
        builder.Property(x => x.CountryCode).HasMaxLength(2);
        builder.Property(x => x.CityCode).HasMaxLength(20);
    }
}
