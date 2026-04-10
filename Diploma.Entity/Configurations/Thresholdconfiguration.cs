using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diploma.Entity.Configurations;

public class ThresholdConfiguration : IEntityTypeConfiguration<ThresholdConfig>
{
    public void Configure(EntityTypeBuilder<ThresholdConfig> b)
    {
        b.ToTable("thresholds");

        b.HasKey(t => t.Id);
        b.Property(t => t.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(t => t.Metric).HasColumnName("metric").HasMaxLength(32).IsRequired();
        b.Property(t => t.Value).HasColumnName("value");
        b.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        b.Property(t => t.DeviceId).HasColumnName("device_id");

        // Уникальность: одна метрика на устройство (не глобально)
        b.HasIndex(t => new { t.DeviceId, t.Metric })
            .IsUnique()
            .HasDatabaseName("ix_thresholds_device_metric");

        // FK на Device
        b.HasOne(t => t.Device)
            .WithMany(d => d.ThresholdConfigs)
            .HasForeignKey(t => t.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed убран — пороги создаются при регистрации устройства,
        // т.к. требуют DeviceId. Заполни через миграцию или AdminController.
    }
}