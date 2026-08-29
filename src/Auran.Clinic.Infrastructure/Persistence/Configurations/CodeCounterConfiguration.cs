using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClinicEntity = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class CodeCounterConfiguration : IEntityTypeConfiguration<CodeCounter>
{
    public void Configure(EntityTypeBuilder<CodeCounter> builder)
    {
        builder.Property(x => x.Scope)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.CodeType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Prefix)
            .HasMaxLength(20);

        builder.HasIndex(x => new { x.Scope, x.ClinicId, x.CodeType, x.Prefix, x.Year })
            .IsUnique();

        builder.HasOne<ClinicEntity>()
            .WithMany()
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_CodeCounters_ScopeClinic",
            "([Scope] = 'Platform' AND [ClinicId] IS NULL) OR ([Scope] = 'Clinic' AND [ClinicId] IS NOT NULL)"));
    }
}
