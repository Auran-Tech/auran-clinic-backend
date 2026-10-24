using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class UserProfilePhotoConfiguration : IEntityTypeConfiguration<UserProfilePhoto>
{
    public void Configure(EntityTypeBuilder<UserProfilePhoto> builder)
    {
        builder.Property(photo => photo.ContentType).HasMaxLength(20).IsRequired();
        builder.Property(photo => photo.ImageData).IsRequired();
        builder.HasIndex(photo => new { photo.ClinicId, photo.UserId }).IsUnique();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(photo => new { photo.UserId, photo.ClinicId })
            .HasPrincipalKey(user => new { user.Id, user.ClinicId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
