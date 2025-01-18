using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReservationSystem.Models.DTO
{
    public class ReservationDto
    {
        public int Id { get; set; }

        [Range(1, 30, ErrorMessage = "Table number must be between 1 and 30.")]
        public int? TableNumber { get; set; }

        [DataType(DataType.DateTime, ErrorMessage = "Invalid date format.")]
        [FutureDate(ErrorMessage = "Reservation time must be in the future.")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? ReservationTime { get; set; }

        [Required]
        [Range(1, 20, ErrorMessage = "Number of guests must be between 1 and 20.")]
        public int NumberOfGuests { get; set; }

        [StringLength(100, ErrorMessage = "Guest name cannot be longer than 100 characters.")]
        public string GuestName { get; set; }
    }

    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value is DateTime dateTime)
            {
                return dateTime > DateTime.Now;
            }
            return true;
        }
    }

    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm"));
        }
    }


}
