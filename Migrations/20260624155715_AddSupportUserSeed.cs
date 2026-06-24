using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bachelor_s_Point.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportUserSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM dbo.Users
    WHERE Email = 'support@bachelorspoint.com'
)
BEGIN
    INSERT INTO dbo.Users
    (
        Address,
        DateOfBirth,
        Email,
        FullName,
        IsPaymentVerified,
        LastLogin,
        PasswordHash,
        PhoneNumber,
        ProfilePicturePath,
        RoleId,
        UserName
    )
    VALUES
    (
        'Bachelor''s Point Official Support',
        NULL,
        'support@bachelorspoint.com',
        'Bachelor''s Point Support',
        1,
        NULL,
        'AQAAAAIAAYagAAAAEHX0M1Zu1VXdU2V7P5K6pzKrEJnLmWVb3Q8xBPbv9Y7aH2Nk4T1wR6Zc0So3Ye2Nqw==',
        NULL,
        NULL,
        1,
        'Support'
    );
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM dbo.Users
WHERE Email = 'support@bachelorspoint.com';
");
        }
    }
}