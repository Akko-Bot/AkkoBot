using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class Alias3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "type",
                table: "aliases");

            migrationBuilder.AddColumn<bool>(
                name: "is_dynamic",
                table: "aliases",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_dynamic",
                table: "aliases");

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "aliases",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
