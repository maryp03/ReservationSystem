namespace ReservationSystem.Models.DTO
{
    public class TableFilterDto
    {
        public int? NumberOfGuests { get; set; } 
        public DateTime? ReservationTime { get; set; }
        public DateTime? AvailableFrom { get; set; } 
        public DateTime? AvailableUntil { get; set; }
    }
}
