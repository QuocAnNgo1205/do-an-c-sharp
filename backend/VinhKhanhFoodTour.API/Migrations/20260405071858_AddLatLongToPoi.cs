using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinhKhanhFoodTour.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLatLongToPoi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Pois",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Pois",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Pois",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Pois");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Pois");
        }
    }
}
