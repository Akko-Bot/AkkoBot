using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class MessageSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "default_prefix",
                table: "bot_config",
                newName: "bot_prefix");

            migrationBuilder.RenameColumn(
                name: "default_language",
                table: "bot_config",
                newName: "ok_color");

            migrationBuilder.AddColumn<string>(
                name: "error_color",
                table: "bot_config",
                type: "varchar(6)",
                maxLength: 6,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "locale",
                table: "bot_config",
                type: "varchar(6)",
                maxLength: 6,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "use_embed",
                table: "bot_config",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "error_color",
                table: "bot_config");

            migrationBuilder.DropColumn(
                name: "locale",
                table: "bot_config");

            migrationBuilder.DropColumn(
                name: "use_embed",
                table: "bot_config");

            migrationBuilder.RenameColumn(
                name: "ok_color",
                table: "bot_config",
                newName: "default_language");

            migrationBuilder.RenameColumn(
                name: "bot_prefix",
                table: "bot_config",
                newName: "default_prefix");
        }
    }
}
