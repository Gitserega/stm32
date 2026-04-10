namespace Diploma.Entity;

public class Measurement
{
    public long Id { get; set; }
    public DateTime ReceivedAt { get; set; }
 
    /* Порядковый номер пакета от STM32 — для детекции потерь */
    public int PacketNumber { get; set; }
 
    /* Миллисекунды с момента старта STM32 */
    public long DeviceTimestamp { get; set; }
 
    /* Флаг готовности baseline на устройстве */
    public bool BaselineReady { get; set; }
 
    /* Ось Z */
    public double Z_Rms    { get; set; }
    public double Z_Crest  { get; set; }
    public double Z_Bear   { get; set; }
    public double Z_Gear   { get; set; }
    public double Z_Freq   { get; set; }
 
    /* Ось X */
    public double X_Rms    { get; set; }
    public double X_Crest  { get; set; }
    public double X_Bear   { get; set; }
    public double X_Gear   { get; set; }
    public double X_Freq   { get; set; }
 
    /* Ось Y */
    public double Y_Rms    { get; set; }
    public double Y_Crest  { get; set; }
    public double Y_Bear   { get; set; }
    public double Y_Gear   { get; set; }
    public double Y_Freq   { get; set; }
 
    /* Навигационное свойство */
    public List<Alert> Alerts { get; set; } = new List<Alert>();
    public long DeviceId { get; set; }
    public Device Device { get; set; }
}