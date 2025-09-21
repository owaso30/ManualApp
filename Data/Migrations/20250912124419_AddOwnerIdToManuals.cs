using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManualApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerIdToManuals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Manuals",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Manuals_OwnerId",
                table: "Manuals",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Manuals_AspNetUsers_OwnerId",
                table: "Manuals",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Manuals_AspNetUsers_OwnerId",
                table: "Manuals");

            migrationBuilder.DropIndex(
                name: "IX_Manuals_OwnerId",
                table: "Manuals");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Manuals");
        }
    }
}
