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
                    Seats = t.Seats,
                    IsAvailable = t.IsAvailable
                })
                .ToListAsync();

            return Ok(tables);
        }

        [HttpGet("{tableNumber}")]
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
                    Seats = t.Seats,
                    IsAvailable = t.IsAvailable
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
                Seats = tableDto.Seats,
                IsAvailable = tableDto.IsAvailable,
            };

            _context.Tables.Add(table);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTableByNumber), new { tableNumber = table.TableNumber }, table);
        }

        [HttpPut("{tableNumber}")]
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

            table.Seats = tableDto.Seats;
            table.IsAvailable = tableDto.IsAvailable;
            table.UpdateDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{tableNumber}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PatchTable(int tableNumber, [FromBody] JsonPatchDocument<TableDto> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest(new { message = "Invalid patch document." });
            }

            var table = await _context.Tables
                .Where(t => t.TableNumber == tableNumber)
                .Select(t => new TableDto
                {
                    Id = t.Id,
                    TableNumber = t.TableNumber,
                    Seats = t.Seats,
                    IsAvailable = t.IsAvailable
                })
                .FirstOrDefaultAsync();

            if (table == null)
            {
                return NotFound(new { message = "Table not found." });
            }

            patchDoc.ApplyTo(table, ModelState);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var tableToUpdate = await _context.Tables.FirstOrDefaultAsync(t => t.TableNumber == tableNumber);
            if (tableToUpdate == null)
            {
                return NotFound(new { message = "Table not found." });
            }
            tableToUpdate.Seats = table.Seats;
            tableToUpdate.IsAvailable = table.IsAvailable;
            tableToUpdate.UpdateDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }



        [HttpDelete("{tableNumber}")]
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

    }
}
