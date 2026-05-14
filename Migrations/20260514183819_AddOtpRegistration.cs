using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bachelor_s_Point.Migrations
{
    /// <inheritdoc />
    public partial class AddOtpRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Users",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PendingRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetRoleId = table.Column<int>(type: "int", nullable: false),
                    OtpHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtpExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingRegistrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingRegistrations_Email",
                table: "PendingRegistrations",
                column: "Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingRegistrations");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");
        }
    }
}
