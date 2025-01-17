using Xunit;
using Moq;
using FluentAssertions;
using ReservationSystem.Controllers;
using ReservationSystem.Models.DTO; 
using ReservationSystem.Models; 
using Microsoft.EntityFrameworkCore;
using ReservationSystem.Data;
using Microsoft.AspNetCore.Mvc;

public class TableControllerTests
{
    [Fact]
    public async Task GetTableByNumber_ShouldReturnCorrectTable()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReservationSystemContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        using (var context = new ReservationSystemContext(options))
        {
            context.Tables.Add(new Table { Id = 1, TableNumber = 1, Seats = 4 });
            context.SaveChanges();
        }

        using (var context = new ReservationSystemContext(options))
        {
            var controller = new TableController(context);

            // Act
            var result = await controller.GetTableByNumber(1);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var table = okResult.Value as TableDto;
            table.Should().NotBeNull();
            table.TableNumber.Should().Be(1);
            table.Seats.Should().Be(4);
        }
    }


    [Fact]
    public async Task GetTableByNumber_ShouldReturnNotFound_WhenTableDoesNotExist()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReservationSystemContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase_NotFound")
            .Options;

        using (var context = new ReservationSystemContext(options))
        {
            
        }

        using (var context = new ReservationSystemContext(options))
        {
            var controller = new TableController(context);

            // Act
            var result = await controller.GetTableByNumber(1);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }
    }


    [Fact]
    public async Task GetAllTables_ShouldReturnListOfTables()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReservationSystemContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase_ListOfTables")
            .Options;

        using (var context = new ReservationSystemContext(options))
        {
            context.Tables.AddRange(
                new Table { Id = 1, TableNumber = 1, Seats = 4 },
                new Table { Id = 2, TableNumber = 2, Seats = 6 }
            );
            context.SaveChanges();
        }

        using (var context = new ReservationSystemContext(options))
        {
            var controller = new TableController(context);

            // Act
            var result = await controller.GetAllTables();

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var tableList = okResult.Value as List<TableDto>;
            tableList.Should().NotBeNull();
            tableList.Count.Should().Be(2);
            tableList[0].TableNumber.Should().Be(1);
            tableList[0].Seats.Should().Be(4);
            tableList[1].TableNumber.Should().Be(2);
            tableList[1].Seats.Should().Be(6);
        }
    }

    [Fact]
    public async Task AddTable_ShouldCreateTable_WhenDataIsValid()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReservationSystemContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase_AddTable")
            .Options;

        using (var context = new ReservationSystemContext(options))
        {
            var controller = new TableController(context);
            var newTable = new TableDto
            {
                TableNumber = 1,
                Seats = 4
            };

            // Act
            var result = await controller.AddTable(newTable);

            // Assert
            result.Result.Should().BeOfType<CreatedAtActionResult>();
            var createdAtResult = result.Result as CreatedAtActionResult;
            var createdTable = createdAtResult.Value as Table;
            createdTable.Should().NotBeNull();
            createdTable.TableNumber.Should().Be(1);
            createdTable.Seats.Should().Be(4);
            var tablesInDb = await context.Tables.ToListAsync();
            tablesInDb.Count.Should().Be(1);
            tablesInDb.First().TableNumber.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateTable_ShouldUpdateTable_WhenDataIsValid()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReservationSystemContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase_UpdateTable")
            .Options;

        using (var context = new ReservationSystemContext(options))
        {
            context.Tables.Add(new Table { Id = 1, TableNumber = 1, Seats = 4 });
            context.SaveChanges();
        }

        using (var context = new ReservationSystemContext(options))
        {
            var controller = new TableController(context);
            var updatedTable = new TableDto
            {
                TableNumber = 1,
                Seats = 6
            };

            // Act
            var result = await controller.UpdateTable(1, updatedTable);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            var tableInDb = await context.Tables.FirstOrDefaultAsync(t => t.TableNumber == 1);
            tableInDb.Should().NotBeNull();
            tableInDb.Seats.Should().Be(6);
        }
    }

    [Fact]
    public async Task DeleteTable_ShouldRemoveTable_WhenTableExists()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReservationSystemContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase_DeleteTable")
            .Options;

        using (var context = new ReservationSystemContext(options))
        {
            context.Tables.Add(new Table { Id = 1, TableNumber = 1, Seats = 4 });
            context.SaveChanges();
        }

        using (var context = new ReservationSystemContext(options))
        {
            var controller = new TableController(context);

            // Act
            var result = await controller.DeleteTable(1);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            var tableInDb = await context.Tables.FirstOrDefaultAsync(t => t.TableNumber == 1);
            tableInDb.Should().BeNull();
        }
    }




}
