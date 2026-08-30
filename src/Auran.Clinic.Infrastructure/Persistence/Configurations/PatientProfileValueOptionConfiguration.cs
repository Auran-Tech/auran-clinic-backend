using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class PatientProfileValueOptionConfiguration : IEntityTypeConfiguration<PatientProfileValueOption>
{
    public void Configure(EntityTypeBuilder<PatientProfileValueOption> builder)
    {
        builder.HasIndex(x => new { x.ClinicId, x.PatientProfileValueId, x.OptionId }).IsUnique();
        builder.HasOne<PatientProfileValue>()
            .WithMany()
            .HasForeignKey(x => x.PatientProfileValueId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PatientProfileFieldOption>()
            .WithMany()
            .HasForeignKey(x => x.OptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
