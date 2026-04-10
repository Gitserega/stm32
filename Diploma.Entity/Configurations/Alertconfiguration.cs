using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diploma.Entity.Configurations;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> b)
    {
        b.ToTable("alerts");

        b.HasKey(a => a.Id);
        b.Property(a => a.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        b.Property(a => a.TriggeredAt)
            .HasColumnName("triggered_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()");

        b.Property(a => a.Severity)
            .HasColumnName("severity")
            .HasConversion<string>();   // "Warning" / "Critical"

        b.Property(a => a.Axis)
            .HasColumnName("axis")
            .HasConversion<string>();   // "X" / "Y" / "Z"

        b.Property(a => a.Metric)
            .HasColumnName("metric")
            .HasConversion<string>();   // "Crest" / "Bearing" / "Gear"

        b.Property(a => a.Value).HasColumnName("value");
        b.Property(a => a.Threshold).HasColumnName("threshold");
        b.Property(a => a.MeasurementId).HasColumnName("measurement_id");

        // Связь с Measurement настроена в MeasurementConfiguration (каскадное удаление)

        b.HasIndex(a => a.TriggeredAt).HasDatabaseName("ix_alerts_triggered_at");
        b.HasIndex(a => a.MeasurementId).HasDatabaseName("ix_alerts_measurement_id");
    }
}