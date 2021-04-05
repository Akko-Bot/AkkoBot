using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class RepeatedCommands : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_private",
                table: "auto_commands");

            migrationBuilder.AlterColumn<decimal>(
                name: "guild_id",
                table: "auto_commands",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "guild_id",
                table: "auto_commands",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AddColumn<bool>(
                name: "is_private",
                table: "auto_commands",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}