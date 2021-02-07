using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MSTC.Web.Model
{
    public class CancelEventRequest
    {
        public int? MemberId { get; set; }
        public int EventSlotId { get; set; }
    }
}