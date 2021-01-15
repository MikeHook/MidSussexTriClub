using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mstc.Core.Domain
{
    public class EventType
    {
        public EventType()
        {
            EventSlots = new List<EventSlot>();
        }

        public int Id { get; set; }
        public string Name { get; set; }

        public bool IsBookable => EventSlots.Any(e => e.IsFutureEvent && e.HasSpace);

        public List<EventSlot> EventSlots { get; set; }
    }
}
