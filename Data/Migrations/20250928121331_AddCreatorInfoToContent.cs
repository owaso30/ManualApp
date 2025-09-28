using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManualApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatorInfoToContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Contents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatorId",
                table: "Contents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Contents_CreatorId",
                table: "Contents",
                column: "CreatorId");

            // 既存のContentレコードのCreatorIdをManualのOwnerIdと同じ値に設定
            migrationBuilder.Sql("UPDATE \"Contents\" SET \"CreatorId\" = (SELECT \"OwnerId\" FROM \"Manuals\" WHERE \"Manuals\".\"ManualId\" = \"Contents\".\"ManualId\") WHERE \"CreatorId\" = ''");
            
            // 存在しないユーザーIDをデフォルトユーザーに設定
            migrationBuilder.Sql("UPDATE \"Contents\" SET \"CreatorId\" = (SELECT \"Id\" FROM \"AspNetUsers\" LIMIT 1) WHERE \"CreatorId\" NOT IN (SELECT \"Id\" FROM \"AspNetUsers\")");

            migrationBuilder.AddForeignKey(
                name: "FK_Contents_AspNetUsers_CreatorId",
                table: "Contents",
                column: "CreatorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contents_AspNetUsers_CreatorId",
                table: "Contents");

            migrationBuilder.DropIndex(
                name: "IX_Contents_CreatorId",
                table: "Contents");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Contents");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "Contents");
        }
    }
}
