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
        public int EventPageId { get; set; }
        public int EventTypeId { get; set; }
        public DateTime Date { get; set; }
        public int Cost { get; set; }
        public int MaxParticipants { get; set; }

        public bool IsFutureEvent => Date.Date > DateTime.Now.Date;
        public bool HasSpace => EventParticipants.Count < MaxParticipants;
        public int SpacesRemaining => MaxParticipants - EventParticipants.Count;

        public List<EventParticipant> EventParticipants { get; set; }
    }
}
