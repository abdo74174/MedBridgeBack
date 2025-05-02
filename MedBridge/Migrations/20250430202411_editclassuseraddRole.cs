using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MedBridge.Migrations
{
    /// <inheritdoc />
    public partial class editclassuseraddRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "RefreshTokens");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "users",
                newName: "KindOfWork");

            migrationBuilder.RenameColumn(
                name: "MessageId",
                table: "ContactUs",
                newName: "Id");

            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "created",
                table: "ContactUs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "MedicalSpecialties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalSpecialties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkType", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "MedicalSpecialties",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Cardiology" },
                    { 2, "Neurology" },
                    { 3, "Pediatrics" },
                    { 4, "Orthopedics" },
                    { 5, "Dermatology" }
                });

            migrationBuilder.InsertData(
                table: "WorkType",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Doctor" },
                    { 2, "Merchant" },
                    { 3, "MedicalTrader" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicalSpecialties");

            migrationBuilder.DropTable(
                name: "WorkType");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "users");

            migrationBuilder.DropColumn(
                name: "created",
                table: "ContactUs");

            migrationBuilder.RenameColumn(
                name: "KindOfWork",
                table: "users",
                newName: "Role");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ContactUs",
                newName: "MessageId");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "RefreshTokens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
