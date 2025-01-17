using Microsoft.AspNetCore.Authorization;
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

                    if (reservationTime < now)
                    {
                        return BadRequest(new { message = "Reservations cannot be made in the past." });
                    }

                    if (reservationTime.TimeOfDay < openingTime || reservationTime.TimeOfDay >= lastReservationTime)
                    {
                        return BadRequest(new { message = "Reservations can only be made between 10:00 and 22:00." });
                    }

                    bool isTableReserved = existingReservations.Any(r =>
                        reservationTime < r.ReservationTime.AddHours(1) && r.ReservationTime <= reservationTime);

                    if (isTableReserved)
                    {
                        return Conflict(new { message = "Table is already reserved for the specified time." });
                    }

                    reservation.ReservationTime = reservationTime;
                }
                else
                {
                    DateTime potentialStartTime = now.Date == DateTime.UtcNow.Date
                        ? now.AddHours(1).AddMinutes(1)
                        : now.Date + openingTime;

                    foreach (var existingReservation in existingReservations)
                    {
                        if ((existingReservation.ReservationTime - potentialStartTime).TotalHours >= 1)
                        {
                            break;
                        }

                        potentialStartTime = existingReservation.ReservationTime.AddHours(1);

                        if (potentialStartTime.TimeOfDay >= lastReservationTime)
                        {
                            potentialStartTime = potentialStartTime.Date.AddDays(1) + openingTime;
                        }
                    }

                    if (potentialStartTime.TimeOfDay >= lastReservationTime)
                    {
                        return NotFound(new { message = "No available times for the specified table within business hours." });
                    }

                    reservation.ReservationTime = potentialStartTime;
                }

                reservation.TableId = table.Id;
            }
            else
            {
                if (reservationDto.ReservationTime.HasValue)
                {
                    var requestedTime = reservationDto.ReservationTime.Value;

                    if (requestedTime.TimeOfDay < openingTime || requestedTime.TimeOfDay >= lastReservationTime)
                    {
                        return BadRequest(new { message = "Reservations can only be made between 10:00 and 22:00." });
                    }

                    var availableTable = await _context.Tables
                        .Where(t => t.Seats >= reservationDto.NumberOfGuests)
                        .FirstOrDefaultAsync(t => !_context.Reservations
                            .Where(r => r.TableId == t.Id)
                            .Any(r => requestedTime < r.ReservationTime.AddHours(1) && r.ReservationTime <= requestedTime));

                    if (availableTable == null)
                    {
                        return NotFound(new { message = "No available tables for the specified time and number of guests." });
                    }

                    reservation.ReservationTime = requestedTime;
                    reservation.TableId = availableTable.Id;
                }
                else
                {
                    var availableTables = await _context.Tables
                        .Where(t => t.Seats >= reservationDto.NumberOfGuests)
                        .ToListAsync();

                    if (!availableTables.Any())
                    {
                        return NotFound(new { message = "No available tables for the specified number of guests." });
                    }

                    DateTime? globalEarliestTime = null;
                    int? selectedTableId = null;

                    foreach (var table in availableTables)
                    {
                        var existingReservations = await _context.Reservations
                            .Where(r => r.TableId == table.Id)
                            .OrderBy(r => r.ReservationTime)
                            .ToListAsync();

                        DateTime potentialStartTime = now.Date == DateTime.UtcNow.Date
                            ? now.AddHours(1).AddMinutes(1)
                            : now.Date + openingTime;

                        foreach (var existingReservation in existingReservations)
                        {
                            if ((existingReservation.ReservationTime - potentialStartTime).TotalHours >= 1)
                            {
                                break;
                            }

                            potentialStartTime = existingReservation.ReservationTime.AddHours(1);

                            if (potentialStartTime.TimeOfDay >= lastReservationTime)
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

                    if (globalEarliestTime.Value.TimeOfDay >= lastReservationTime)
                    {
                        return NotFound(new { message = "No available times for the specified number of guests within business hours." });
                    }

                    reservation.ReservationTime = globalEarliestTime.Value;
                    reservation.TableId = selectedTableId.Value;
                }
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



        [Authorize]
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
