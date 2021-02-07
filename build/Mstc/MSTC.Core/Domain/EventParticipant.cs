namespace Mstc.Core.Domain
{
    public class EventParticipant
    {
        public int Id { get; set; }
        public int EventSlotId { get; set; }
        public int MemberId { get; set; }
        public int AmountPaid { get; set; }
    }
}