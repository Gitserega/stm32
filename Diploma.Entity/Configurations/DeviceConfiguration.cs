using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diploma.Entity.Configurations;

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> b)
    {
        b.ToTable("devices");

        b.HasKey(d => d.Id);
        b.Property(d => d.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(d => d.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        b.Property(d => d.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        b.Property(d => d.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        // Device → Measurements (один-ко-многим)
        b.HasMany(d => d.Measurements)
            .WithOne(m => m.Device)
            .HasForeignKey(m => m.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Device → ThresholdConfigs (один-ко-многим)
        b.HasMany(d => d.ThresholdConfigs)
            .WithOne(t => t.Device)
            .HasForeignKey(t => t.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}