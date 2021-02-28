namespace Mstc.Core.Domain
{
    public class EventParticipant
    {
        public int Id { get; set; }
        public int EventSlotId { get; set; }
        public int MemberId { get; set; }
        public int AmountPaid { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string RaceDistance { get; set; }
    }
}