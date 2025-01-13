namespace ReservationSystem.Models.DTO
{
    public class ReservationDto
    {
        public int Id { get; set; }
        public int TableId { get; set; }
        public DateTime ReservationTime { get; set; }
        public int NumberOfGuests { get; set; }
        public string? GuestName { get; set; }
    }
}
