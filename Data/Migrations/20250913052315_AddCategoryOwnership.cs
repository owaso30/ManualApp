using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManualApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Categories",
                type: "text",
                nullable: true);

            // 既存のデフォルトカテゴリーを更新
            migrationBuilder.Sql("UPDATE \"Categories\" SET \"IsDefault\" = true, \"OwnerId\" = NULL WHERE \"CategoryId\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_OwnerId",
                table: "Categories",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_AspNetUsers_OwnerId",
                table: "Categories",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_AspNetUsers_OwnerId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_OwnerId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Categories");
        }
    }
}
