using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diploma.Entity.Configurations;

public class ThresholdConfiguration : IEntityTypeConfiguration<ThresholdConfig>
{
    public void Configure(EntityTypeBuilder<ThresholdConfig> b)
    {
        b.ToTable("thresholds");
 
        b.HasKey(t => t.Id);
        b.Property(t => t.Id)       .HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(t => t.Metric)   .HasColumnName("metric").HasMaxLength(32);
        b.Property(t => t.Value)    .HasColumnName("value");
        b.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
 
        b.HasIndex(t => t.Metric).IsUnique().HasDatabaseName("ix_thresholds_metric");
 
        /* Seed — начальные значения порогов */
        b.HasData(
            new ThresholdConfig { Id = 1, Metric = "crest",   Value = 4.0, UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) },
            new ThresholdConfig { Id = 2, Metric = "bearing",  Value = 0.05, UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) },
            new ThresholdConfig { Id = 3, Metric = "gear",     Value = 0.05, UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) }
        );
    }
}