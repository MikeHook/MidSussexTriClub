using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mstc.Core.Domain
{
    public class EventSlot
    {
        public EventSlot()
        {
            EventParticipants = new List<EventParticipant>();
        }

        public int Id { get; set; }
        public int EventTypeId { get; set; }
        public DateTime Date { get; set; }
        public decimal Cost { get; set; }
        public int MaxParticipants { get; set; }

        public bool IsFutureEvent => Date.Date > DateTime.Now.Date;
        public bool HasSpace => EventParticipants.Count < MaxParticipants;

        public List<EventParticipant> EventParticipants { get; set; }
    }
}
