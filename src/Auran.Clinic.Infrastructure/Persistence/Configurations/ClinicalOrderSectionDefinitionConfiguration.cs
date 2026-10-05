using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public class ClinicalOrderSectionDefinitionConfiguration : IEntityTypeConfiguration<ClinicalOrderSectionDefinition>
{
    public void Configure(EntityTypeBuilder<ClinicalOrderSectionDefinition> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(64);
        builder.Property(x => x.SectionType).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => new { x.ClinicId, x.Code }).IsUnique();
    }
}
