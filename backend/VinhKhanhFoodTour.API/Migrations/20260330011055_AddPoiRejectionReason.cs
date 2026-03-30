using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinhKhanhFoodTour.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPoiRejectionReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Pois",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Pois");
        }
    }
}
