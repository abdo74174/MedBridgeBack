using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedBridge.Migrations
{
    /// <inheritdoc />
    public partial class addDelioveryPerson : Migration
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

            migrationBuilder.AddColumn<string>(
                name: "HeraImageUrl",
                table: "users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "users",
                type: "bit",
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
                name: "HeraImageUrl",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "users");

            migrationBuilder.CreateTable(
                name: "DeliveryPersons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryPersons", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_DeliveryPersons_DeliveryPersonId",
                table: "Orders",
                column: "DeliveryPersonId",
                principalTable: "DeliveryPersons",
                principalColumn: "Id");
        }
    }
}
