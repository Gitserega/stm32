namespace Diploma.Entity;

public class Device
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
    public List<Measurement> Measurements { get; set; }
    // public long ThresholdConfigId { get; set; }
    public List<ThresholdConfig> ThresholdConfigs { get; set; }
}