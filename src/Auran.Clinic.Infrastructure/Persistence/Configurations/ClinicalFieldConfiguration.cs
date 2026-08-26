using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public class ClinicalFieldConfiguration : IEntityTypeConfiguration<ClinicalField>
{
    public void Configure(EntityTypeBuilder<ClinicalField> builder) =>
        builder.Property(x => x.FieldType).HasConversion<string>().HasMaxLength(32);
}
