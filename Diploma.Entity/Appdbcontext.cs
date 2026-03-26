using Diploma.Entity.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Diploma.Entity;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
 
    public DbSet<Measurement>    Measurements { get; set; }
    public DbSet<Alert>          Alerts       { get; set; }
    public DbSet<ThresholdConfig> Thresholds  { get; set; }
 
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new MeasurementConfiguration());
        modelBuilder.ApplyConfiguration(new AlertConfiguration());
        modelBuilder.ApplyConfiguration(new ThresholdConfiguration());
    }
}