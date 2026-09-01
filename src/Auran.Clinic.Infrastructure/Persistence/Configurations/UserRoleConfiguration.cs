using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.HasIndex(x => new { x.ClinicId, x.UserId, x.RoleId }).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(userRole => new { userRole.UserId, userRole.ClinicId })
            .HasPrincipalKey(user => new { user.Id, user.ClinicId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
