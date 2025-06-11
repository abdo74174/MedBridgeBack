using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedBridge.Migrations
{
    /// <inheritdoc />
    public partial class add_craedetinordds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Coupons");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Coupons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiscountPercentage = table.Column<double>(type: "float", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    OrderIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coupons", x => x.Id);
                });
        }
    }
}
