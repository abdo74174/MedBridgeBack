using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedBridge.Migrations
{
    /// <inheritdoc />
    public partial class editRequstStatusy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_DeliveryPersons_DeliveryPersonId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "DeliveryPersons");

            migrationBuilder.AddColumn<string>(
                name: "CardImageUrl",
                table: "users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "users",
                type: "nvarchar(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "users",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestStatus",
                table: "users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_users_DeliveryPersonId",
                table: "Orders",
                column: "DeliveryPersonId",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_users_DeliveryPersonId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CardImageUrl",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "users");

            migrationBuilder.DropColumn(
                name: "RequestStatus",
                table: "users");

            migrationBuilder.CreateTable(
                name: "DeliveryPersons",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CardImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HeraImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    KindOfWork = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryPersons", x => x.id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_DeliveryPersons_DeliveryPersonId",
                table: "Orders",
                column: "DeliveryPersonId",
                principalTable: "DeliveryPersons",
                principalColumn: "id");
        }
    }
}
