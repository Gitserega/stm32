namespace Diploma.Api.Models;

/* Десериализация входящего JSON от STM32
 * {"ts":12583,"n":42,"z":{...},"x":{...},"y":{...},"bl":1} */
public class MqttPayload
{
    public long   ts  { get; set; }
    public int    n   { get; set; }
    public AxisData z { get; set; } = new();
    public AxisData x { get; set; } = new();
    public AxisData y { get; set; } = new();
    public int    bl  { get; set; }
}
 
public class AxisData
{
    public double rms   { get; set; }
    public double crest { get; set; }
    public double bear  { get; set; }
    public double gear  { get; set; }
    public double f     { get; set; }
}