using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class Warnpl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "warnings",
                comment: "Stores warnings issued to users on servers.");

            migrationBuilder.AlterTable(
                name: "warn_punishments",
                comment: "Stores punishments to be automatically given once a user reaches a certain amount of warnings.");

            migrationBuilder.AlterTable(
                name: "timers",
                comment: "Stores actions that need to be performed at some point in the future.");

            migrationBuilder.AlterTable(
                name: "occurrencies",
                comment: "Stores how many times a user got punished in a server.");

            migrationBuilder.AlterTable(
                name: "muted_users",
                comment: "Stores data about users that got muted in a specific server.");

            migrationBuilder.AlterTable(
                name: "log_config",
                comment: "Stores data and settings related to how the bot logs command usage.");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "interval",
                table: "warn_punishments",
                type: "interval",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "interval",
                table: "warn_punishments");

            migrationBuilder.AlterTable(
                name: "warnings",
                oldComment: "Stores warnings issued to users on servers.");

            migrationBuilder.AlterTable(
                name: "warn_punishments",
                oldComment: "Stores punishments to be automatically given once a user reaches a certain amount of warnings.");

            migrationBuilder.AlterTable(
                name: "timers",
                oldComment: "Stores actions that need to be performed at some point in the future.");

            migrationBuilder.AlterTable(
                name: "occurrencies",
                oldComment: "Stores how many times a user got punished in a server.");

            migrationBuilder.AlterTable(
                name: "muted_users",
                oldComment: "Stores data about users that got muted in a specific server.");

            migrationBuilder.AlterTable(
                name: "log_config",
                oldComment: "Stores data and settings related to how the bot logs command usage.");
        }
    }
}
