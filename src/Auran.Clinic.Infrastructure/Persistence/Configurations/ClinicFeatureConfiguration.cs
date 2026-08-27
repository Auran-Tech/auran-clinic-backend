using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class ClinicFeatureConfiguration : IEntityTypeConfiguration<ClinicFeature>
{
    public void Configure(EntityTypeBuilder<ClinicFeature> builder)
    {
        builder.Property(x => x.ConfigurationJson).HasMaxLength(8000);
        builder.HasIndex(x => new { x.ClinicId, x.FeatureDefinitionId }).IsUnique();
    }
}
