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
                    Id = t.Id,
                    TableNumber = t.TableNumber,
                    Seats = t.Seats
                })
                .ToListAsync();

            return Ok(tables);
        }

        [HttpGet("by-table/{tableNumber}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TableDto>> GetTableByNumber(int tableNumber)
        {
            var table = await _context.Tables
                .Where(t => t.TableNumber == tableNumber)
                .Select(t => new TableDto
                {
                    Id = t.Id,
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
                TableNumber = tableDto.TableNumber,
                Seats = tableDto.Seats
            };

            _context.Tables.Add(table);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTableByNumber), new { tableNumber = table.TableNumber }, table);
        }

        [HttpPut("by-table/{tableId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateTable(int tableId, [FromBody] TableDto tableDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var table = await _context.Tables.FirstOrDefaultAsync(t => t.Id == tableId);  
            if (table == null)
            {
                return NotFound(new { message = "Table not found." });
            }

            var hasReservations = await _context.Reservations.AnyAsync(r => r.TableId == tableId);  
            if (hasReservations && table.Seats != tableDto.Seats)
            {
                return BadRequest(new { message = "Cannot change the number of seats because there are active reservations for this table." });
            }

            table.TableNumber = tableDto.TableNumber; 
            table.Seats = tableDto.Seats;
            table.UpdateDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }







        [HttpDelete("by-table/{tableNumber}")]
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

        [HttpGet("tables")]
        public async Task<IActionResult> GetTables([FromQuery] TableFilterDto filter)
        {
            var query = _context.Tables.AsQueryable();

            if (filter.NumberOfGuests.HasValue)
            {
                query = query.Where(t => t.Seats >= filter.NumberOfGuests.Value);
            }

            if (filter.AvailableFrom.HasValue)
            {
                var availableFrom = filter.AvailableFrom.Value;
                query = query.Where(t => t.Reservations.All(r => r.ReservationTime < availableFrom || r.ReservationTime.AddHours(1) <= availableFrom));
            }

            if (filter.AvailableUntil.HasValue)
            {
                var availableUntil = filter.AvailableUntil.Value;
                query = query.Where(t => t.Reservations.All(r => r.ReservationTime >= availableUntil || r.ReservationTime.AddHours(1) >= availableUntil));
            }

            if (filter.ReservationTime.HasValue)
            {
                var reservationTime = filter.ReservationTime.Value;
                query = query.Where(t => !t.Reservations.Any(r => r.ReservationTime == reservationTime));
            }

            var tables = await query.ToListAsync();

            return Ok(tables);
        }



    }
}
    