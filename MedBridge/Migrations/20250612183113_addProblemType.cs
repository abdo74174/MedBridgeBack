using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedBridge.Migrations
{
    /// <inheritdoc />
    public partial class addProblemType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactUs_users_UserId1",
                table: "ContactUs");

            migrationBuilder.DropIndex(
                name: "IX_ContactUs_UserId1",
                table: "ContactUs");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "ContactUs");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "ContactUs",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ProblemType",
                table: "ContactUs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactUs_users_UserId",
                table: "ContactUs");

            migrationBuilder.DropIndex(
                name: "IX_ContactUs_UserId",
                table: "ContactUs");

            migrationBuilder.DropColumn(
                name: "ProblemType",
                table: "ContactUs");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ContactUs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

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
    }
}
