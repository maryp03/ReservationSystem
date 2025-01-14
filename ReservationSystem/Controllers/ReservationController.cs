using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationSystem.Data;
using ReservationSystem.Models;
using ReservationSystem.Models.DTO;

namespace ReservationSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationController : ControllerBase
    {
        private readonly ReservationSystemContext _context;

        public ReservationController(ReservationSystemContext context)
        {
            _context = context;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetAllReservations()
        {
            var reservations = await _context.Reservations
                .Select(r => new ReservationDto
                {
                    Id = r.Id,
                    TableNumber = r.TableNumber,
                    ReservationTime = r.ReservationTime,
                    NumberOfGuests = r.NumberOfGuests,
                    GuestName = r.GuestName
                })
                .ToListAsync();

            return Ok(reservations);
        }

        [HttpGet("by-table/{tableNumber}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetReservationsByTableNumber(int tableNumber)
        {
            var reservations = await _context.Reservations
                .Where(r => r.TableNumber == tableNumber)
                .Select(r => new ReservationDto
                {
                    Id = r.Id,
                    TableNumber = r.TableNumber,
                    ReservationTime = r.ReservationTime,
                    NumberOfGuests = r.NumberOfGuests,
                    GuestName = r.GuestName
                })
                .ToListAsync();

            if (!reservations.Any())
            {
                return NotFound(new { message = "No reservations found for the specified table number." });
            }

            return Ok(reservations);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Reservation>> AddReservation([FromBody] ReservationDto reservationDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (reservationDto.NumberOfGuests <= 0)
            {
                return BadRequest(new { message = "The number of guests must be greater than 0." });
            }

            Reservation reservation = new Reservation();

            if (reservationDto.TableNumber.HasValue && reservationDto.TableNumber.Value > 0)
            {
                var table = await _context.Tables.FirstOrDefaultAsync(t => t.TableNumber == reservationDto.TableNumber.Value);

                if (table == null)
                {
                    return NotFound(new { message = "Table not found." });
                }

                if (table.Seats < reservationDto.NumberOfGuests)
                {
                    return BadRequest(new { message = "The table does not have enough seats for the specified number of guests." });
                }

                var existingReservations = await _context.Reservations
                    .Where(r => r.TableNumber == reservationDto.TableNumber)
                    .ToListAsync();

                if (reservationDto.ReservationTime.HasValue)
                {
                    bool isTableReserved = existingReservations.Any(r =>
                        r.ReservationTime >= reservationDto.ReservationTime.Value.AddHours(-1) &&
                        r.ReservationTime <= reservationDto.ReservationTime.Value.AddHours(1));

                    if (isTableReserved)
                    {
                        return Conflict(new { message = "Table is already reserved for the specified time or within one hour of the reservation time." });
                    }

                    reservation.ReservationTime = reservationDto.ReservationTime.Value;
                }
                else
                {
                    DateTime currentTime = DateTime.UtcNow;
                    DateTime? availableTime = null;

                    foreach (var res in existingReservations.OrderBy(r => r.ReservationTime))
                    {
                        if ((res.ReservationTime - currentTime).TotalHours > 1)
                        {
                            availableTime = currentTime.AddHours(1);
                            break;
                        }
                        currentTime = res.ReservationTime.AddHours(1);
                    }

                    reservation.ReservationTime = availableTime ?? currentTime;
                }

                reservation.TableNumber = reservationDto.TableNumber.Value;
            }
            else
            {
                var availableTables = await _context.Tables
                    .Where(t => t.Seats >= reservationDto.NumberOfGuests)
                    .ToListAsync();

                DateTime? globalEarliestTime = null;
                int? selectedTable = null;

                foreach (var table in availableTables)
                {
                    var existingReservations = await _context.Reservations
                        .Where(r => r.TableNumber == table.TableNumber)
                        .OrderBy(r => r.ReservationTime)
                        .ToListAsync();

                    DateTime potentialStartTime = DateTime.UtcNow;

                    foreach (var existingReservation in existingReservations)
                    {
                        if ((existingReservation.ReservationTime - potentialStartTime).TotalHours > 1)
                        {
                            break;
                        }

                        potentialStartTime = existingReservation.ReservationTime.AddHours(1);
                    }

                    if (globalEarliestTime == null || potentialStartTime < globalEarliestTime)
                    {
                        globalEarliestTime = potentialStartTime;
                        selectedTable = table.TableNumber;
                    }
                }

                if (globalEarliestTime == null || selectedTable == null)
                {
                    return NotFound(new { message = "No available tables for the specified number of guests and time." });
                }

                reservation.TableNumber = selectedTable.Value;
                reservation.ReservationTime = globalEarliestTime.Value;
            }

            reservation.NumberOfGuests = reservationDto.NumberOfGuests;
            reservation.GuestName = reservationDto.GuestName;
            reservation.CreateDate = DateTime.UtcNow;

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReservationsByTableNumber), new { tableNumber = reservation.TableNumber }, reservation);
        }







        [HttpDelete("by-Id/{Id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteReservationsByTableNumber(int Id)
        {
            var reservations = await _context.Reservations
                .Where(r => r.Id == Id)
                .ToListAsync();

            if (!reservations.Any())
            {
                return NotFound(new { message = "No reservations found for the specified table number." });
            }

            _context.Reservations.RemoveRange(reservations);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
