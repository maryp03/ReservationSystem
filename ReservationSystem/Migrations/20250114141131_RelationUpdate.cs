using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class RelationUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Tables_TableNumber",
                table: "Reservations");

            migrationBuilder.RenameColumn(
                name: "TableNumber",
                table: "Reservations",
                newName: "TableId");

            migrationBuilder.RenameIndex(
                name: "IX_Reservations_TableNumber_ReservationTime",
                table: "Reservations",
                newName: "IX_Reservations_TableId_ReservationTime");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Tables_TableId",
                table: "Reservations",
                column: "TableId",
                principalTable: "Tables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Tables_TableId",
                table: "Reservations");

            migrationBuilder.RenameColumn(
                name: "TableId",
                table: "Reservations",
                newName: "TableNumber");

            migrationBuilder.RenameIndex(
                name: "IX_Reservations_TableId_ReservationTime",
                table: "Reservations",
                newName: "IX_Reservations_TableNumber_ReservationTime");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Tables_TableNumber",
                table: "Reservations",
                column: "TableNumber",
                principalTable: "Tables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
