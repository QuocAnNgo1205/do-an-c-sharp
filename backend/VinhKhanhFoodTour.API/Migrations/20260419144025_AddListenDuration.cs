using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinhKhanhFoodTour.API.Migrations
{
    /// <inheritdoc />
    public partial class AddListenDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ListenDurationSeconds",
                table: "NarrationLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ListenDurationSeconds",
                table: "NarrationLogs");
        }
    }
}
