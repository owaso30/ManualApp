using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManualApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatorInfoToManualAndCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatorId",
                table: "Manuals",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatorId",
                table: "Categories",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Manuals_CreatorId",
                table: "Manuals",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CreatorId",
                table: "Categories",
                column: "CreatorId");

            // 既存のManualレコードのCreatorIdをOwnerIdと同じ値に設定
            migrationBuilder.Sql("UPDATE \"Manuals\" SET \"CreatorId\" = \"OwnerId\" WHERE \"CreatorId\" = ''");
            
            // 存在しないユーザーIDをnullに設定（ManualテーブルではCreatorIdはNOT NULLなので、デフォルトユーザーを設定）
            migrationBuilder.Sql("UPDATE \"Manuals\" SET \"CreatorId\" = (SELECT \"Id\" FROM \"AspNetUsers\" LIMIT 1) WHERE \"CreatorId\" NOT IN (SELECT \"Id\" FROM \"AspNetUsers\")");
            
            // 既存のCategoryレコードのCreatorIdをOwnerIdと同じ値に設定（OwnerIdがnullでない場合）
            migrationBuilder.Sql("UPDATE \"Categories\" SET \"CreatorId\" = \"OwnerId\" WHERE \"OwnerId\" IS NOT NULL");
            
            // OwnerIdがnullのCategoryレコードのCreatorIdもnullに設定
            migrationBuilder.Sql("UPDATE \"Categories\" SET \"CreatorId\" = NULL WHERE \"OwnerId\" IS NULL");

            // 存在しないユーザーIDをnullに設定
            migrationBuilder.Sql("UPDATE \"Categories\" SET \"CreatorId\" = NULL WHERE \"CreatorId\" IS NOT NULL AND \"CreatorId\" NOT IN (SELECT \"Id\" FROM \"AspNetUsers\")");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_AspNetUsers_CreatorId",
                table: "Categories",
                column: "CreatorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Manuals_AspNetUsers_CreatorId",
                table: "Manuals",
                column: "CreatorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_AspNetUsers_CreatorId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Manuals_AspNetUsers_CreatorId",
                table: "Manuals");

            migrationBuilder.DropIndex(
                name: "IX_Manuals_CreatorId",
                table: "Manuals");

            migrationBuilder.DropIndex(
                name: "IX_Categories_CreatorId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "Manuals");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "Categories");
        }
    }
}
