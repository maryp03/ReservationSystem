using System.ComponentModel.DataAnnotations;

namespace ReservationSystem.Models.DTO
{
    public class TableDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Table number is required.")]
        [Range(1, 30, ErrorMessage = "Table number must be between 1 and 30.")]
        public int TableNumber { get; set; }

        [Required(ErrorMessage = "Seats are required.")]
        [Range(1, 20, ErrorMessage = "Seats must be between 1 and 20.")]
        public int Seats { get; set; }
        public bool IsAvailable { get; set; }
    }
}
