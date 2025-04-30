using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedBridge.Migrations
{
    /// <inheritdoc />
    public partial class FixProfileImageType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old ProfileImage column
            migrationBuilder.DropColumn(
                name: "ProfileImage",
                table: "users");

            // Add the new ProfileImage column with string type
            migrationBuilder.AddColumn<string>(
                name: "ProfileImage",
                table: "users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the string version if rolling back
            migrationBuilder.DropColumn(
                name: "ProfileImage",
                table: "users");

            // Re-add the old binary version
            migrationBuilder.AddColumn<byte[]>(
                name: "ProfileImage",
                table: "users",
                type: "varbinary(max)",
                nullable: true);
        }
    }
}
