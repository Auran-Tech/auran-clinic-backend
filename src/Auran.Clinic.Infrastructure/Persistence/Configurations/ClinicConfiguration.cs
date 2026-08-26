using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClinicEntity = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.Infrastructure.Persistence.Configurations;

public class ClinicConfiguration : IEntityTypeConfiguration<ClinicEntity>
{
    public void Configure(EntityTypeBuilder<ClinicEntity> builder)
    {
        builder.ToTable("Clinics");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.Property(x => x.PrimaryColor).HasMaxLength(20);
        builder.Property(x => x.SecondaryColor).HasMaxLength(20);
        builder.Property(x => x.FontFamily).HasMaxLength(100);
        builder.Property(x => x.TimeZoneId).HasMaxLength(100);
        builder.Property(x => x.PatientNumberPrefix).HasMaxLength(20);
    }
}
