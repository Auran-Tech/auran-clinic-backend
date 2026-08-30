using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class CodeCounterConfiguration : IEntityTypeConfiguration<CodeCounter>
{
    public void Configure(EntityTypeBuilder<CodeCounter> builder)
    {
        builder.HasIndex(x => new { x.ClinicId, x.CodeType, x.ScopeKey }).IsUnique();
        builder.Property(x => x.CodeType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ScopeKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
