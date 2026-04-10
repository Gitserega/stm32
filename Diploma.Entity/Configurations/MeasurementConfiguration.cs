using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Diploma.Entity.Configurations;

public class MeasurementConfiguration : IEntityTypeConfiguration<Measurement>
{
    public void Configure(EntityTypeBuilder<Measurement> b)
    {
        b.ToTable("measurements");
 
        b.HasKey(m => m.Id);
        b.Property(m => m.Id).HasColumnName("id").UseIdentityByDefaultColumn();
 
        b.Property(m => m.ReceivedAt)
            .HasColumnName("received_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()");
 
        b.Property(m => m.PacketNumber)   .HasColumnName("packet_number");
        b.Property(m => m.DeviceTimestamp).HasColumnName("device_ts");
        b.Property(m => m.BaselineReady)  .HasColumnName("baseline_ready");
 
        b.Property(m => m.Z_Rms)  .HasColumnName("z_rms");
        b.Property(m => m.Z_Crest).HasColumnName("z_crest");
        b.Property(m => m.Z_Bear) .HasColumnName("z_bear");
        b.Property(m => m.Z_Gear) .HasColumnName("z_gear");
        b.Property(m => m.Z_Freq) .HasColumnName("z_freq");
 
        b.Property(m => m.X_Rms)  .HasColumnName("x_rms");
        b.Property(m => m.X_Crest).HasColumnName("x_crest");
        b.Property(m => m.X_Bear) .HasColumnName("x_bear");
        b.Property(m => m.X_Gear) .HasColumnName("x_gear");
        b.Property(m => m.X_Freq) .HasColumnName("x_freq");
 
        b.Property(m => m.Y_Rms)  .HasColumnName("y_rms");
        b.Property(m => m.Y_Crest).HasColumnName("y_crest");
        b.Property(m => m.Y_Bear) .HasColumnName("y_bear");
        b.Property(m => m.Y_Gear) .HasColumnName("y_gear");
        b.Property(m => m.Y_Freq) .HasColumnName("y_freq");
 
        /* Индекс для быстрой выборки последних N измерений */
        b.HasIndex(m => m.ReceivedAt).HasDatabaseName("ix_measurements_received_at");
 
        b.HasMany(m => m.Alerts)
            .WithOne(a => a.Measurement)
            .HasForeignKey(a => a.MeasurementId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(m => m.Device)
            .WithMany(d => d.Measurements)
            .HasForeignKey(m => m.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}