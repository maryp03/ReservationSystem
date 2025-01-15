using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;


namespace ReservationSystem.Models.DTO
{
    public class TableDto
    {

        [Range(1, 30, ErrorMessage = "Table number must be between 1 and 30.")]
        public int? TableNumber { get; set; }

        [Required(ErrorMessage = "Seats are required.")]
        [Range(1, 20, ErrorMessage = "Seats must be between 1 and 20.")]
        public int Seats { get; set; }
    }
}
