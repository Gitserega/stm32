namespace Diploma.Entity;

public enum AlertSeverity { Warning, Critical }
public enum AlertAxis { X, Y, Z }
public enum AlertMetric { Rms, Crest, Bearing, Gear }

public class Alert
{
    public long Id { get; set; }
    public DateTime TriggeredAt { get; set; }

    public AlertSeverity Severity { get; set; }
    public AlertAxis Axis { get; set; }
    public AlertMetric Metric { get; set; }

    public double Value { get; set; }
    public double Threshold { get; set; }


    public long MeasurementId { get; set; }
    public Measurement Measurement { get; set; } = null!;

   
}