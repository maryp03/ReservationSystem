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
                    TableNumber = r.Table.TableNumber,  
                    ReservationTime = r.ReservationTime,
                    NumberOfGuests = r.NumberOfGuests,
                    GuestName = r.GuestName
                })
                .ToListAsync();

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


            var openingTime = TimeSpan.FromHours(10); 
            var lastReservationTime = TimeSpan.FromHours(22); 
            var now = DateTime.UtcNow;


            DateTime adjustedNow = now.AddMinutes(60 - now.Minute).AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);
            adjustedNow = adjustedNow.TimeOfDay < openingTime ? adjustedNow.Date + openingTime : adjustedNow;

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
                    .Where(r => r.TableId == table.Id)
                    .OrderBy(r => r.ReservationTime)
                    .ToListAsync();

                if (reservationDto.ReservationTime.HasValue)
                {
                    var reservationTime = reservationDto.ReservationTime.Value;

                    if (reservationTime < adjustedNow)
                    {
                        return BadRequest(new { message = "Reservations cannot be made in the past." });
                    }
                    if (reservationTime.TimeOfDay < openingTime || reservationTime.TimeOfDay > lastReservationTime)
                    {
                        return BadRequest(new { message = "Reservations can only be made between 10:00 and 22:00." });
                    }

                    bool isTableReserved = existingReservations.Any(r =>
                        r.ReservationTime >= reservationTime.AddHours(-1) &&
                        r.ReservationTime <= reservationTime.AddHours(1));

                    if (isTableReserved)
                    {
                        return Conflict(new { message = "Table is already reserved for the specified time or within one hour of the reservation time." });
                    }

                    reservation.ReservationTime = reservationTime;
                }
                else
                {
                    DateTime currentTime = adjustedNow.AddHours(1);
                    DateTime? availableTime = null;

                    foreach (var res in existingReservations.OrderBy(r => r.ReservationTime))
                    {
                        if ((res.ReservationTime - currentTime).TotalHours > 1)
                        {
                            availableTime = currentTime;
                            break;
                        }
                        currentTime = res.ReservationTime.AddHours(1);

                        if (currentTime.TimeOfDay > lastReservationTime)
                        {
                            currentTime = currentTime.Date.AddDays(1) + openingTime;
                        }
                    }

                    availableTime ??= currentTime;

                    if (availableTime.Value.TimeOfDay > lastReservationTime)
                    {
                        availableTime = availableTime.Value.Date.AddDays(1) + openingTime;
                    }

                    reservation.ReservationTime = availableTime.Value;
                }

                reservation.TableId = table.Id;
            }
            else
            {
                var availableTables = await _context.Tables
                    .Where(t => t.Seats >= reservationDto.NumberOfGuests)
                    .ToListAsync();

                DateTime? globalEarliestTime = null;
                int? selectedTableId = null;

                foreach (var table in availableTables)
                {
                    var existingReservations = await _context.Reservations
                        .Where(r => r.TableId == table.Id)
                        .OrderBy(r => r.ReservationTime)
                        .ToListAsync();

                    DateTime potentialStartTime = adjustedNow.AddHours(1);

                    foreach (var existingReservation in existingReservations)
                    {
                        if ((existingReservation.ReservationTime - potentialStartTime).TotalHours > 1)
                        {
                            break;
                        }

                        potentialStartTime = existingReservation.ReservationTime.AddHours(1);

                        if (potentialStartTime.TimeOfDay > lastReservationTime)
                        {
                            potentialStartTime = potentialStartTime.Date.AddDays(1) + openingTime;
                        }
                    }

                    if (globalEarliestTime == null || potentialStartTime < globalEarliestTime)
                    {
                        globalEarliestTime = potentialStartTime;
                        selectedTableId = table.Id;
                    }
                }

                if (globalEarliestTime == null || selectedTableId == null)
                {
                    return NotFound(new { message = "No available tables for the specified number of guests and time." });
                }

                if (globalEarliestTime.Value.TimeOfDay > lastReservationTime)
                {
                    globalEarliestTime = globalEarliestTime.Value.Date.AddDays(1) + openingTime;
                }

                reservation.ReservationTime = globalEarliestTime.Value;
                reservation.TableId = selectedTableId.Value;
            }

            reservation.NumberOfGuests = reservationDto.NumberOfGuests;
            reservation.GuestName = reservationDto.GuestName;
            reservation.CreateDate = now.AddHours(1);

            _context.Reservations.Add(reservation);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error saving reservation.", details = ex.InnerException?.Message });
            }

            var tableForReservation = await _context.Tables
                .FirstOrDefaultAsync(t => t.Id == reservation.TableId);

            var response = new
            {
                reservation.Id,
                TableNumber = tableForReservation?.TableNumber,
                reservation.ReservationTime,
                reservation.NumberOfGuests,
                reservation.GuestName,
                CreateDate = reservation.CreateDate
            };

            return CreatedAtAction(nameof(GetAllReservations), null, response);
        }

        [HttpDelete("by-Id/{Id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteReservationsById(int Id)
        {
            var reservations = await _context.Reservations
                .Where(r => r.Id == Id)
                .ToListAsync();

            if (!reservations.Any())
            {
                return NotFound(new { message = "No reservations found for the specified Id." });
            }

            _context.Reservations.RemoveRange(reservations);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
