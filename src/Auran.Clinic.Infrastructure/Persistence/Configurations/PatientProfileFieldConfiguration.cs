using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public class PatientProfileFieldConfiguration : IEntityTypeConfiguration<PatientProfileField>
{
    public void Configure(EntityTypeBuilder<PatientProfileField> builder) =>
        builder.Property(x => x.FieldType).HasConversion<string>().HasMaxLength(32);
}
