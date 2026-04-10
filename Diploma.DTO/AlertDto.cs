using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diploma.DTO
{
    public class AlertDto
    {
        public long Id { get; set; }
        public DateTime TriggeredAt { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Axis { get; set; } = string.Empty;
        public string Metric { get; set; } = string.Empty;
        public double Value { get; set; }
        public double Threshold { get; set; }
        public long MeasurementId { get; set; }
    }
}
