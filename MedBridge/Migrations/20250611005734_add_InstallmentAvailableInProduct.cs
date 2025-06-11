using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedBridge.Migrations
{
    /// <inheritdoc />
    public partial class add_InstallmentAvailableInProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InstallmentAvailable",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstallmentAvailable",
                table: "Products");
        }
    }
}
