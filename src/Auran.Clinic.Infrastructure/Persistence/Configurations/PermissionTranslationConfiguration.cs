using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class PermissionTranslationConfiguration : IEntityTypeConfiguration<PermissionTranslation>
{
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<PermissionTranslation> builder)
    {
        builder.HasIndex(x => new { x.PermissionId, x.LanguageCode }).IsUnique();
        builder.Property(x => x.LanguageCode).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();

        builder.HasData(Permissions.All.SelectMany(definition => new[]
        {
            new PermissionTranslation
            {
                Id = PermissionSeedIds.Translation(definition.Key, "en"),
                PermissionId = PermissionSeedIds.Permission(definition.Key),
                LanguageCode = "en",
                Description = definition.EnglishDescription,
                CreatedDate = SeedDate
            },
            new PermissionTranslation
            {
                Id = PermissionSeedIds.Translation(definition.Key, "ar"),
                PermissionId = PermissionSeedIds.Permission(definition.Key),
                LanguageCode = "ar",
                Description = definition.ArabicDescription,
                CreatedDate = SeedDate
            }
        }));
    }
}
