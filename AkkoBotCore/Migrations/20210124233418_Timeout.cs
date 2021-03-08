using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace AkkoBot.Migrations
{
    public partial class Timeout : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "interactive_timeout",
                table: "guild_configs",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "interactive_timeout",
                table: "bot_config",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "interactive_timeout",
                table: "guild_configs");

            migrationBuilder.DropColumn(
                name: "interactive_timeout",
                table: "bot_config");
        }
    }
}