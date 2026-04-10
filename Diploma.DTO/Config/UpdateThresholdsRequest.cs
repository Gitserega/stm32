using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diploma.DTO.Config
{
    public class UpdateThresholdsRequest
    {
        public long DeviceId { get; set; }
        public double? Rms { get; set; }
        public double? Crest { get; set; }
        public double? Bearing { get; set; }
        public double? Gear { get; set; }
    }
}
