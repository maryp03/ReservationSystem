using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationSystem.Data;
using ReservationSystem.Models;
using ReservationSystem.Models.DTO;

namespace ReservationSystem.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class TableController : ControllerBase
    {
        private readonly ReservationSystemContext _context;

        public TableController(ReservationSystemContext context)
        {
            _context = context;
        }
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<TableDto>>> GetAllTables()
        {
            var tables = await _context.Tables
                .Select(t => new TableDto
                {
                    TableNumber = t.TableNumber,
                    Seats = t.Seats
                })
                .ToListAsync();

            return Ok(tables);
        }

        [HttpGet("by-number/{tableNumber}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TableDto>> GetTableByNumber(int tableNumber)
        {
            var table = await _context.Tables
                .Where(t => t.TableNumber == tableNumber)
                .Select(t => new TableDto
                {
                    TableNumber = t.TableNumber,
                    Seats = t.Seats
                })
                .FirstOrDefaultAsync();

            if (table == null)
            {
                return NotFound(new { message = "Table not found." });
            }

            return Ok(table);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Table>> AddTable([FromBody] TableDto tableDto)
        {
            if (tableDto.TableNumber == null)
            {
                return BadRequest(new { message = "Table number is required." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await _context.Tables.AnyAsync(t => t.TableNumber == tableDto.TableNumber))
            {
                return Conflict(new { message = "Table number must be unique." });
            }

            var table = new Table
            {
                TableNumber = tableDto.TableNumber.Value,  
                Seats = tableDto.Seats
            };

            _context.Tables.Add(table);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTableByNumber), new { tableNumber = table.TableNumber }, table);
        }


        [HttpPut("by-number/{tableNumber}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateTable(int tableNumber, [FromBody] TableDto tableDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var table = await _context.Tables.FirstOrDefaultAsync(t => t.TableNumber == tableNumber);
            if (table == null)
            {
                return NotFound(new { message = "Table not found." });
            }

            var hasReservations = await _context.Reservations.AnyAsync(r => r.TableId == table.Id);
            if (hasReservations && table.Seats != tableDto.Seats)
            {
                return BadRequest(new { message = "Cannot change the number of seats because there are active reservations for this table." });
            }

            if (tableDto.TableNumber.HasValue && table.TableNumber != tableDto.TableNumber.Value)
            {
                table.TableNumber = tableDto.TableNumber.Value;
            }
            table.Seats = tableDto.Seats;
            table.UpdateDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }



        [HttpDelete("by-number/{tableNumber}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteTable(int tableNumber)
        {
            var table = await _context.Tables.FirstOrDefaultAsync(t => t.TableNumber == tableNumber);
            if (table == null)
            {
                return NotFound(new { message = "Table not found." });
            }

            _context.Tables.Remove(table);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        [HttpGet("tables-by-seats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTablesBySeats([FromQuery] int numberOfGuests)
        {
            var tables = await _context.Tables
                .Where(t => t.Seats >= numberOfGuests)
                .Select(t => new
                {
                    t.Id,
                    t.TableNumber,
                    t.Seats,
                    t.CreateDate,
                    t.UpdateDate
                })
                .ToListAsync();

            if (!tables.Any())
            {
                return NotFound(new { message = "No tables found with the required number of seats." });
            }

            return Ok(tables);
        }


        [HttpGet("available-tables")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAvailableTables(
        [FromQuery] int numberOfGuests,
        [FromQuery] DateTime availableFrom,
        [FromQuery] DateTime availableUntil)
        {
            if (availableFrom < DateTime.UtcNow || availableUntil < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Dates cannot be in the past." });
            }

            var openingTime = TimeSpan.FromHours(10);
            var closingTime = TimeSpan.FromHours(23);
            var lastReservationTime = closingTime - TimeSpan.FromHours(1);

            availableFrom = availableFrom.Date + (availableFrom.TimeOfDay < openingTime ? openingTime : availableFrom.TimeOfDay);
            availableUntil = availableUntil.Date + (availableUntil.TimeOfDay > closingTime ? closingTime : availableUntil.TimeOfDay);

            var tables = await _context.Tables
                .Where(t => t.Seats >= numberOfGuests)
                .ToListAsync();

            var availableTables = new List<object>();

            foreach (var table in tables)
            {
                var reservations = await _context.Reservations
                    .Where(r => r.TableId == table.Id &&
                                r.ReservationTime < availableUntil &&
                                r.ReservationTime.AddHours(1) > availableFrom)
                    .OrderBy(r => r.ReservationTime)
                    .ToListAsync();

                var freeTimeSlots = new List<(DateTime start, DateTime end)>();

                var currentStart = availableFrom;
                foreach (var reservation in reservations)
                {
                    var reservationStart = reservation.ReservationTime;
                    var reservationEnd = reservation.ReservationTime.AddHours(1);

                    if (reservationStart - currentStart >= TimeSpan.FromHours(1))
                    {
                        freeTimeSlots.Add((currentStart, reservationStart));
                    }

                    currentStart = reservationEnd;
                }

                if (availableUntil - currentStart >= TimeSpan.FromHours(1))
                {
                    freeTimeSlots.Add((currentStart, availableUntil));
                }

                var splitTimeSlots = SplitTimeSlotsByOpeningHours(freeTimeSlots, openingTime, lastReservationTime, closingTime);

                foreach (var slot in splitTimeSlots)
                {
                    if (slot.end - slot.start >= TimeSpan.FromHours(1))
                    {
                        availableTables.Add(new
                        {
                            table.TableNumber,
                            table.Seats,
                            AvailableFrom = slot.start,
                            AvailableUntil = slot.end
                        });
                    }
                }
            }

            if (!availableTables.Any())
            {
                return NotFound(new { message = "No tables available in the specified time range." });
            }

            return Ok(availableTables);
        }

        private List<(DateTime start, DateTime end)> SplitTimeSlotsByOpeningHours(
            List<(DateTime start, DateTime end)> timeSlots,
            TimeSpan openingTime,
            TimeSpan lastReservationTime,
            TimeSpan closingTime)
        {
            var result = new List<(DateTime start, DateTime end)>();

            foreach (var slot in timeSlots)
            {
                var currentStart = slot.start;

                while (currentStart < slot.end)
                {
                    var currentDate = currentStart.Date;

                    var adjustedStart = currentStart.TimeOfDay < openingTime ? currentDate + openingTime : currentStart;
                    var adjustedEnd = currentDate + closingTime;

                    if (slot.end.Date > currentDate)
                    {
                        adjustedEnd = currentDate + closingTime;
                    }
                    else
                    {
                        adjustedEnd = slot.end.TimeOfDay > closingTime ? currentDate + closingTime : slot.end;
                    }

                    if (adjustedStart < adjustedEnd)
                    {
                        if (result.Any() && result.Last().end == adjustedStart)
                        {
                            var mergedSlot = (result.Last().start, adjustedEnd);
                            result[result.Count - 1] = mergedSlot;
                        }
                        else
                        {
                            result.Add((adjustedStart, adjustedEnd));
                        }
                    }

                    currentStart = currentDate.AddDays(1) + openingTime;
                }
            }

            for (int i = 0; i < result.Count - 1; i++)
            {
                if (result[i].end == result[i + 1].start)
                {
                    result[i] = (result[i].start, result[i + 1].end);
                    result.RemoveAt(i + 1);
                    i--;
                }
            }

            if (result.Any() && result.Last().end.TimeOfDay == lastReservationTime)
            {
                var lastSlot = result.Last();
                result[result.Count - 1] = (lastSlot.start, lastSlot.start.Date + closingTime);
            }

            return result;
        }



        [HttpGet("reservations-by-time")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetReservationsByTime(
        [FromQuery] DateTime availableFrom,
        [FromQuery] DateTime availableUntil)
        {
            var reservations = await _context.Reservations
                .Where(r => r.ReservationTime >= availableFrom && r.ReservationTime <= availableUntil)
                .Include(r => r.Table) 
                .ToListAsync();

            if (!reservations.Any())
            {
                return NotFound(new { message = "No reservations found in the specified time range." });
            }

            return Ok(reservations.Select(r => new
            {
                r.Id,
                r.ReservationTime,
                r.NumberOfGuests,
                r.GuestName,
                TableNumber = r.Table.TableNumber
            }));
        }




    }
}
    