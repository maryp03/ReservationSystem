using Xunit;
using Moq;
using FluentAssertions;
using ReservationSystem.Controllers;
using ReservationSystem.Models.DTO; 
using ReservationSystem.Models; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationSystem.Data;

public class ReservationControllerTests
{
    [Fact]
    public async Task GetAllReservations_ShouldReturnListOfReservations()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReservationSystemContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase_GetAllReservations")
            .Options;

        using (var context = new ReservationSystemContext(options))
        {
            context.Reservations.AddRange(
                new Reservation
                {
                    Id = 1,
                    TableId = 1,
                    ReservationTime = DateTime.UtcNow.AddHours(2),
                    NumberOfGuests = 4,
                    GuestName = "MArys",
                    Table = new Table { TableNumber = 5 }
                },
                new Reservation
                {
                    Id = 2,
                    TableId = 2,
                    ReservationTime = DateTime.UtcNow.AddHours(3),
                    NumberOfGuests = 2,
                    GuestName = "MArys2",
                    Table = new Table { TableNumber = 6 }
                }
            );
            context.SaveChanges();
        }

        using (var context = new ReservationSystemContext(options))
        {
            var controller = new ReservationController(context);

            // Act
            var result = await controller.GetAllReservations();

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var resultList = okResult.Value as List<ReservationDto>;
            resultList.Should().NotBeNull();
            resultList.Count.Should().Be(2);
            resultList[0].GuestName.Should().Be("MArys");
            resultList[1].GuestName.Should().Be("MArys2");
        }
    }

    [Fact]
    public async Task AddReservation_ShouldReturnCreatedResult_WhenSuccessful()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReservationSystemContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase_AddReservation")
            .Options;

        using (var context = new ReservationSystemContext(options))
        {
            context.Tables.Add(new Table { Id = 1, TableNumber = 5, Seats = 4 });
            await context.SaveChangesAsync();
        }

        using (var context = new ReservationSystemContext(options))
        {
            var controller = new ReservationController(context);

            var newReservation = new ReservationDto
            {
                TableNumber = 5,
                ReservationTime = DateTime.UtcNow.AddHours(2),
                NumberOfGuests = 4,
                GuestName = "Marys"
            };

            // Act
            var result = await controller.AddReservation(newReservation);

            // Assert
            Assert.NotNull(result.Result); 
            result.Result.Should().BeOfType<CreatedAtActionResult>();

            var createdAtResult = result.Result as CreatedAtActionResult;
            Assert.NotNull(createdAtResult); 

            var reservationDetails = createdAtResult.Value;
            Assert.NotNull(reservationDetails); 

            var reservationsInDb = await context.Reservations.ToListAsync();
            Assert.Single(reservationsInDb);
            Assert.Equal("Marys", reservationsInDb[0].GuestName);
        }
    }


    [Fact]
    public async Task DeleteReservation_ShouldReturnNoContent_WhenReservationExists()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReservationSystemContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase_DeleteReservation_Exists")
            .Options;

        using (var context = new ReservationSystemContext(options))
        {
            context.Reservations.Add(new Reservation
            {
                Id = 1,
                ReservationTime = DateTime.UtcNow.AddHours(2),
                NumberOfGuests = 4,
                GuestName = "Test Guest"
            });
            context.SaveChanges();
        }

        using (var context = new ReservationSystemContext(options))
        {
            var controller = new ReservationController(context);

            // Act
            var result = await controller.DeleteReservationsById(1);

            // Assert
            Assert.NotNull(result); 
            result.Should().BeOfType<NoContentResult>(); 

            var reservationInDb = await context.Reservations.FirstOrDefaultAsync(r => r.Id == 1);
            Assert.Null(reservationInDb);
        }
    }





    [Fact]
    public async Task DeleteReservation_ShouldReturnNotFound_WhenReservationDoesNotExist()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReservationSystemContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase_DeleteReservation_NotFound")
            .Options;

        using (var context = new ReservationSystemContext(options))
        {
            
        }

        using (var context = new ReservationSystemContext(options))
        {
            var controller = new ReservationController(context);

            // Act
            var result = await controller.DeleteReservationsById(1);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }

}
