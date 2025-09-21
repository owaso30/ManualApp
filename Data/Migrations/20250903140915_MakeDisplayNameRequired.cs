using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManualApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeDisplayNameRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "AspNetUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }
    }
}
