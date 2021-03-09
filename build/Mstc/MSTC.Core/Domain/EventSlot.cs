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
        public string EventTypeName { get; set; }
        public DateTime Date { get; set; }
        public int Cost { get; set; }
        public int MaxParticipants { get; set; }
        public string Distances { get; set; }
        public string IndemnityWaiverDocumentLink { get; set; }
        public string CovidDocumentLink { get; set; }
        public List<string> RaceDistances => Distances?.Split(';').ToList() ?? new List<string>();

        public string DateDisplay => Date.ToString("dddd, dd MMMM yyyy hh:mm tt");
        public bool IsFutureEvent => Date.Date > DateTime.Now.Date;
        public bool HasSpace => Participants < MaxParticipants;
        public int Participants => EventParticipants.Count;
        public int SpacesRemaining => MaxParticipants - Participants;

        public List<EventParticipant> EventParticipants { get; set; }

        public void SetDisances(IEnumerable<string> eventRaceDistances)
        {
            Distances = eventRaceDistances != null && eventRaceDistances.Any() ? string.Join(";", eventRaceDistances) : null;
        }
    }
}
