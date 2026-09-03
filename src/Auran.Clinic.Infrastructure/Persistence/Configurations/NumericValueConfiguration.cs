using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class ClinicalMeasurementConfiguration : IEntityTypeConfiguration<ClinicalMeasurement>
{
    public void Configure(EntityTypeBuilder<ClinicalMeasurement> builder)
    {
        builder.Property(measurement => measurement.NumberValue)
            .HasColumnType("decimal(18,2)");
    }
}

public sealed class PatientProfileValueConfiguration : IEntityTypeConfiguration<PatientProfileValue>
{
    public void Configure(EntityTypeBuilder<PatientProfileValue> builder)
    {
        builder.Property(value => value.NumberValue)
            .HasColumnType("decimal(18,2)");
    }
}
