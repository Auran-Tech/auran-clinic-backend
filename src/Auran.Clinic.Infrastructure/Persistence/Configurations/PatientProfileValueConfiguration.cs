using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class PatientProfileValueConfiguration : IEntityTypeConfiguration<PatientProfileValue>
{
    public void Configure(EntityTypeBuilder<PatientProfileValue> builder)
    {
        builder.HasIndex(x => new { x.ClinicId, x.PatientId, x.FieldId }).IsUnique();
    }
}
