using Microsoft.EntityFrameworkCore;
using ClinicEntity = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.Infrastructure.Persistence;

public class AuranClinicDbContext(DbContextOptions<AuranClinicDbContext> options)
    : DbContext(options)
{
    public DbSet<ClinicEntity> Clinics => Set<ClinicEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuranClinicDbContext).Assembly);
    }
}
