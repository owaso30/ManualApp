using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ManualApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class RestructureTablesWithDefaultCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_Manuals_ManualId",
                table: "Images");

            migrationBuilder.DropForeignKey(
                name: "FK_Manuals_AspNetUsers_OwnerId",
                table: "Manuals");

            migrationBuilder.DropIndex(
                name: "IX_Images_ManualId",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "Manuals");

            migrationBuilder.RenameColumn(
                name: "ManualId",
                table: "Images",
                newName: "ContentId");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Manuals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            // デフォルトのカテゴリを作成
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "Name", "Description" },
                values: new object[] { 1, "デフォルト", "デフォルトカテゴリ" });

            // 既存のManualsのCategoryIdを1に更新
            migrationBuilder.Sql("UPDATE \"Manuals\" SET \"CategoryId\" = 1 WHERE \"CategoryId\" = 0");

            migrationBuilder.CreateTable(
                name: "Contents",
                columns: table => new
                {
                    ContentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    ManualId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contents", x => x.ContentId);
                    table.ForeignKey(
                        name: "FK_Contents_Manuals_ManualId",
                        column: x => x.ManualId,
                        principalTable: "Manuals",
                        principalColumn: "ManualId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Manuals_CategoryId",
                table: "Manuals",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_ContentId",
                table: "Images",
                column: "ContentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contents_ManualId",
                table: "Contents",
                column: "ManualId");

            migrationBuilder.AddForeignKey(
                name: "FK_Images_Contents_ContentId",
                table: "Images",
                column: "ContentId",
                principalTable: "Contents",
                principalColumn: "ContentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Manuals_AspNetUsers_OwnerId",
                table: "Manuals",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Manuals_Categories_CategoryId",
                table: "Manuals",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_Contents_ContentId",
                table: "Images");

            migrationBuilder.DropForeignKey(
                name: "FK_Manuals_AspNetUsers_OwnerId",
                table: "Manuals");

            migrationBuilder.DropForeignKey(
                name: "FK_Manuals_Categories_CategoryId",
                table: "Manuals");

            // デフォルトカテゴリを削除
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 1);

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Contents");

            migrationBuilder.DropIndex(
                name: "IX_Manuals_CategoryId",
                table: "Manuals");

            migrationBuilder.DropIndex(
                name: "IX_Images_ContentId",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Manuals");

            migrationBuilder.RenameColumn(
                name: "ContentId",
                table: "Images",
                newName: "ManualId");

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "Manuals",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Images_ManualId",
                table: "Images",
                column: "ManualId");

            migrationBuilder.AddForeignKey(
                name: "FK_Images_Manuals_ManualId",
                table: "Images",
                column: "ManualId",
                principalTable: "Manuals",
                principalColumn: "ManualId");

            migrationBuilder.AddForeignKey(
                name: "FK_Manuals_AspNetUsers_OwnerId",
                table: "Manuals",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
