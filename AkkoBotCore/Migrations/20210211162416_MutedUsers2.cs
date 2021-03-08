using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace AkkoBot.Migrations
{
    public partial class MutedUsers2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "elapses_at",
                table: "muted_users",
                newName: "elapse_at");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "interactive_timeout",
                table: "bot_config",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0),
                oldClrType: typeof(TimeSpan),
                oldType: "interval",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "elapse_at",
                table: "muted_users",
                newName: "elapses_at");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "interactive_timeout",
                table: "bot_config",
                type: "interval",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "interval");
        }
    }
}