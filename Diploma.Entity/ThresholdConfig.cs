namespace Diploma.Entity;

/* Пороги хранятся в БД — меняются без перепрошивки STM32 */
public class ThresholdConfig
{
    public int    Id        { get; set; }
    public string Metric    { get; set; } = string.Empty;  /* "crest" | "bearing" | "gear" */
    public double Value     { get; set; }
    public DateTime UpdatedAt { get; set; }
}