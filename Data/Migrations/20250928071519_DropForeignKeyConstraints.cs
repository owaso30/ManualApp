using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManualApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropForeignKeyConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Manualテーブルの外部キー制約を削除
            migrationBuilder.DropForeignKey(
                name: "FK_Manuals_AspNetUsers_OwnerId",
                table: "Manuals");

            // Categoryテーブルの外部キー制約を削除
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_AspNetUsers_OwnerId",
                table: "Categories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 外部キー制約を復元
            migrationBuilder.AddForeignKey(
                name: "FK_Manuals_AspNetUsers_OwnerId",
                table: "Manuals",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_AspNetUsers_OwnerId",
                table: "Categories",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
