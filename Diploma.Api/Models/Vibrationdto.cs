namespace Diploma.Api.Models;

/* DTO для SignalR — то что улетает на Blazor дашборд */
public class VibrationDto
{
    public long   MeasurementId   { get; set; }
    public DateTime ReceivedAt    { get; set; }
    public int    PacketNumber    { get; set; }
    public bool   BaselineReady   { get; set; }
 
    public AxisDto Z { get; set; } = new();
    public AxisDto X { get; set; } = new();
    public AxisDto Y { get; set; } = new();
 
    public List<AlertDto> Alerts  { get; set; } = new();
}
 
public class AxisDto
{
    public double Rms   { get; set; }
    public double Crest { get; set; }
    public double Bear  { get; set; }
    public double Gear  { get; set; }
    public double Freq  { get; set; }
}
 
public class AlertDto
{
    public string Axis     { get; set; } = string.Empty;
    public string Metric   { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public double Value    { get; set; }
    public double Threshold { get; set; }
}