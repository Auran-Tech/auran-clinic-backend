using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClinicEntity = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public sealed class FileUploadSessionConfiguration : IEntityTypeConfiguration<FileUploadSession>
{
    public void Configure(EntityTypeBuilder<FileUploadSession> builder)
    {
        builder.Property(x => x.RequestedByActorType).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.OriginalName).HasMaxLength(255);
        builder.Property(x => x.FileExtension).HasMaxLength(20);
        builder.Property(x => x.ContentType).HasMaxLength(200);
        builder.Property(x => x.StorageProvider).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.StorageKey).HasMaxLength(500);
        builder.Property(x => x.UploadTokenHash).HasMaxLength(64);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(x => x.UploadTokenHash).IsUnique();
        builder.HasIndex(x => new { x.ClinicId, x.Status, x.ExpiresAtUtc });
        builder.HasIndex(x => new { x.RequestedByActorType, x.RequestedByActorId });
        builder.HasIndex(x => x.FileId).IsUnique().HasFilter("[FileId] IS NOT NULL");

        builder.HasOne<ClinicEntity>()
            .WithMany()
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FileRecord>()
            .WithMany()
            .HasForeignKey(x => x.FileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
