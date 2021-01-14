using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mstc.Core.Dto
{
    public class EventSlot
    {
        public DateTime eventDate { get; set; }
        public int participants { get; set; }
        public decimal cost { get; set; }
    }
}
