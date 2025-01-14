using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class RelationChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Reservations_TableNumber_ReservationTime",
                table: "Reservations",
                columns: new[] { "TableNumber", "ReservationTime" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Tables_TableNumber",
                table: "Reservations",
                column: "TableNumber",
                principalTable: "Tables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Tables_TableNumber",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_TableNumber_ReservationTime",
                table: "Reservations");
        }
    }
}
