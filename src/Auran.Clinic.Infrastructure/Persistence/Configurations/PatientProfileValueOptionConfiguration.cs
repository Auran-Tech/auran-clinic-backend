using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class PatientProfileValueOptionConfiguration : IEntityTypeConfiguration<PatientProfileValueOption>
{
    public void Configure(EntityTypeBuilder<PatientProfileValueOption> builder) =>
        builder.HasIndex(x => new { x.ClinicId, x.PatientProfileValueId, x.OptionId }).IsUnique();
}
