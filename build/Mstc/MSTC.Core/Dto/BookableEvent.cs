using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mstc.Core.Dto
{
    public class BookableEvent
    {
        public BookableEvent()
        {
            eventSlots = new List<EventSlot>();
        }

        public int eventTypeId { get; set; }
        public string eventTypeName { get; set; }

        public List<EventSlot> eventSlots { get; set; }
    }
}
