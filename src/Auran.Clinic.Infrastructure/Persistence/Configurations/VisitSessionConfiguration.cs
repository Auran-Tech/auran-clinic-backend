using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class VisitSessionConfiguration : IEntityTypeConfiguration<VisitSession>
{
    public void Configure(EntityTypeBuilder<VisitSession> builder)
    {
        builder.HasIndex(x => new { x.ClinicId, x.VisitId })
            .IsUnique()
            .HasFilter("[EndedAtUtc] IS NULL");
    }
}
