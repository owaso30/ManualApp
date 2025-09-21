using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManualApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeManualIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_Manuals_ManualId",
                table: "Images");

            migrationBuilder.AlterColumn<int>(
                name: "ManualId",
                table: "Images",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Images_Manuals_ManualId",
                table: "Images",
                column: "ManualId",
                principalTable: "Manuals",
                principalColumn: "ManualId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_Manuals_ManualId",
                table: "Images");

            migrationBuilder.AlterColumn<int>(
                name: "ManualId",
                table: "Images",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Images_Manuals_ManualId",
                table: "Images",
                column: "ManualId",
                principalTable: "Manuals",
                principalColumn: "ManualId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
