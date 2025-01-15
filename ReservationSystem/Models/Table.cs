using System.Text.Json.Serialization;

namespace ReservationSystem.Models
{
    public class Table
    {
        public int Id { get; set; }
        public int TableNumber { get; set; }
        public int Seats { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdateDate { get; set; } = DateTime.UtcNow;

        public ICollection<Reservation> Reservations { get; set; }
    }
}
