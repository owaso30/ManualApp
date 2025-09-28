using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManualApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExistingCategoriesCreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 既存のカテゴリーレコードのCreatedAtを現在の日時に更新
            migrationBuilder.Sql("UPDATE \"Categories\" SET \"CreatedAt\" = NOW() WHERE \"CreatedAt\" = TIMESTAMPTZ '-infinity'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ロールバック時は何もしない（CreatedAtは削除しない）
        }
    }
}
