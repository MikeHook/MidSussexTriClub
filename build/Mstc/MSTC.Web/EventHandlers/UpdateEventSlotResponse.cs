using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MSTC.Web.EventHandlers
{
    public class UpdateEventSlotResponse
    {
        public int SlotsCreated { get; set; }
        public int SlotsDeleted { get; set; }
        public int SlotsUpdated { get; set; }
    }
}