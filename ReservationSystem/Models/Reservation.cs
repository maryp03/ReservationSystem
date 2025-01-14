using System.Text.Json.Serialization;

namespace ReservationSystem.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        public int TableId { get; set; }
        public DateTime ReservationTime { get; set; }
        public int NumberOfGuests { get; set; }
        public string? GuestName { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;


        public Table Table { get; set; }
    }
}
