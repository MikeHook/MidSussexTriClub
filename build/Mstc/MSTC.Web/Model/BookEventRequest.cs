using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MSTC.Web.Model
{
    public class BookEventRequest
    {
        public string EventTypeName { get; set; }
        public int EventSlotId { get; set; }
        public int Cost { get; set; }
        public string RaceDistance { get; set; }
    }
}