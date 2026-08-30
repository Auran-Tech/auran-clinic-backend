using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class FileRecordConfiguration : IEntityTypeConfiguration<FileRecord>
{
    public void Configure(EntityTypeBuilder<FileRecord> builder)
    {
        builder.Property(x => x.OriginalName).HasMaxLength(255);
        builder.Property(x => x.StoredName).HasMaxLength(255);
        builder.Property(x => x.FileExtension).HasMaxLength(20);
        builder.Property(x => x.ContentType).HasMaxLength(200);
        builder.Property(x => x.StorageProvider).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.StorageKey).HasMaxLength(500);
        builder.Property(x => x.UploadedByActorType).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => x.StorageKey).IsUnique();
        builder.HasIndex(x => new { x.ClinicId, x.UploadedAtUtc });
        builder.HasIndex(x => new { x.UploadedByActorType, x.UploadedByActorId });
    }
}
