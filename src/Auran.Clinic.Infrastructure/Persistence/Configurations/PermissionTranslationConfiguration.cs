using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class PermissionTranslationConfiguration : IEntityTypeConfiguration<PermissionTranslation>
{
    public void Configure(EntityTypeBuilder<PermissionTranslation> builder)
    {
        builder.ToTable("PermissionTranslations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LanguageCode)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(x => new { x.PermissionId, x.LanguageCode })
            .IsUnique();

        builder.HasOne<Permission>()
            .WithMany()
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
