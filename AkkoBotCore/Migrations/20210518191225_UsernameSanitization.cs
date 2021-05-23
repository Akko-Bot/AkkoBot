using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class UsernameSanitization : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "timers",
                comment: "Stores a timer that executes actions at some point in the future.",
                oldComment: "Stores actions that need to be performed at some point in the future.");

            migrationBuilder.AlterTable(
                name: "polls",
                comment: "Stores data related to a server poll.",
                oldComment: "Stores data related to guild polls.");

            migrationBuilder.AlterTable(
                name: "occurrences",
                comment: "Stores the amount of infractions commited by a user in a server.",
                oldComment: "Stores how many times a user got punished in a server.");

            migrationBuilder.AlterTable(
                name: "guild_config",
                comment: "Stores settings and data related to a Discord server.",
                oldComment: "Stores settings related to individual Discord servers.");

            migrationBuilder.AlterTable(
                name: "discord_users",
                comment: "Stores data related to individual Discord users.",
                oldComment: "Stores data and settings related to individual Discord users.");

            migrationBuilder.AlterTable(
                name: "auto_commands",
                comment: "Stores command data and the context it should be automatically sent to.",
                oldComment: "Stores command data and the context it should be sent to.");

            migrationBuilder.AlterColumn<string>(
                name: "stream_url",
                table: "playing_statuses",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "timezone",
                table: "guild_config",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "custom_sanitized_name",
                table: "guild_config",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "sanitize_names",
                table: "guild_config",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "custom_sanitized_name",
                table: "guild_config");

            migrationBuilder.DropColumn(
                name: "sanitize_names",
                table: "guild_config");

            migrationBuilder.AlterTable(
                name: "timers",
                comment: "Stores actions that need to be performed at some point in the future.",
                oldComment: "Stores a timer that executes actions at some point in the future.");

            migrationBuilder.AlterTable(
                name: "polls",
                comment: "Stores data related to guild polls.",
                oldComment: "Stores data related to a server poll.");

            migrationBuilder.AlterTable(
                name: "occurrences",
                comment: "Stores how many times a user got punished in a server.",
                oldComment: "Stores the amount of infractions commited by a user in a server.");

            migrationBuilder.AlterTable(
                name: "guild_config",
                comment: "Stores settings related to individual Discord servers.",
                oldComment: "Stores settings and data related to a Discord server.");

            migrationBuilder.AlterTable(
                name: "discord_users",
                comment: "Stores data and settings related to individual Discord users.",
                oldComment: "Stores data related to individual Discord users.");

            migrationBuilder.AlterTable(
                name: "auto_commands",
                comment: "Stores command data and the context it should be sent to.",
                oldComment: "Stores command data and the context it should be automatically sent to.");

            migrationBuilder.AlterColumn<string>(
                name: "stream_url",
                table: "playing_statuses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "timezone",
                table: "guild_config",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);
        }
    }
}