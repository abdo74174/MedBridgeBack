using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedBridge.Migrations
{
    /// <inheritdoc />
    public partial class addisdeletedtoProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isdeleted",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isdeleted",
                table: "Products");
        }
    }
}
