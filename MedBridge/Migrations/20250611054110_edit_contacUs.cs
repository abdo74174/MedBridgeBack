using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedBridge.Migrations
{
    /// <inheritdoc />
    public partial class edit_contacUs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactUs_users_UserId",
                table: "ContactUs");

            migrationBuilder.DropIndex(
                name: "IX_ContactUs_UserId",
                table: "ContactUs");

            migrationBuilder.RenameColumn(
                name: "created",
                table: "ContactUs",
                newName: "CreatedAt");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ContactUs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "ContactUs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "ContactUs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactUs_UserId1",
                table: "ContactUs",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ContactUs_users_UserId1",
                table: "ContactUs",
                column: "UserId1",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactUs_users_UserId1",
                table: "ContactUs");

            migrationBuilder.DropIndex(
                name: "IX_ContactUs_UserId1",
                table: "ContactUs");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "ContactUs");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "ContactUs");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ContactUs",
                newName: "created");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "ContactUs",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_ContactUs_UserId",
                table: "ContactUs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContactUs_users_UserId",
                table: "ContactUs",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
