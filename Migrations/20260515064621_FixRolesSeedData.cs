using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bachelor_s_Point.Migrations
{
    /// <inheritdoc />
    public partial class FixRolesSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "RoleDescription", "RoleName" },
                values: new object[] { "Regular user — can post and select rooms", "User" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "RoleDescription", "RoleName" },
                values: new object[] { "Default user role", "RoomOwner" });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "RoleDescription", "RoleName" },
                values: new object[] { 3, "Legacy role", "RoomSeeker" });
        }
    }
}
