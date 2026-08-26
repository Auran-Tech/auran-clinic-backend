using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasIndex(x => new { x.ClinicId, x.PatientNumber }).IsUnique();
        builder.HasIndex(x => new { x.ClinicId, x.Phone }).IsUnique();
        builder.Property(x => x.PatientNumber).HasMaxLength(64);
        builder.Property(x => x.FullName).HasMaxLength(256);
        builder.Property(x => x.Phone).HasMaxLength(64);
    }
}
