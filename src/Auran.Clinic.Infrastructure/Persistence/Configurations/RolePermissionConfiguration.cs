using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClinicEntityType = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasIndex(x => new { x.ClinicId, x.RoleId, x.PermissionId }).IsUnique();

        builder.HasOne<ClinicEntityType>()
            .WithMany()
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
