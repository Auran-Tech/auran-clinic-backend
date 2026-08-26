using Auran.Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Persistence;

public class AuranClinicDbContext(DbContextOptions<AuranClinicDbContext> options)
    : DbContext(options)
{
    public DbSet<Clinic> Clinics => Set<Clinic>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuranClinicDbContext).Assembly);
    }
}
