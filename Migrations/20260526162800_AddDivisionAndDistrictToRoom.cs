using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bachelor_s_Point.Migrations
{
    /// <inheritdoc />
    public partial class AddDivisionAndDistrictToRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "Rooms",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Division",
                table: "Rooms",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "District",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "Division",
                table: "Rooms");
        }
    }
}