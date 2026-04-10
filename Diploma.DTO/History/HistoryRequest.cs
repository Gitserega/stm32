using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diploma.DTO.History
{
    public class HistoryRequest
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Parameter { get; set; } // "Rms", "Crest", "Bear", "Gear"
    }
}
