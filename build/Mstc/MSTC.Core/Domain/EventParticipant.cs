namespace Mstc.Core.Domain
{
    public class EventParticipant
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public decimal AmountPaid { get; set; }
    }
}