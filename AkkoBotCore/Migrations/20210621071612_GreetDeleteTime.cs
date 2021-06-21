using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class GreetDeleteTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "farewell_delete_time",
                table: "gatekeeping",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "greet_delete_time",
                table: "gatekeeping",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "farewell_delete_time",
                table: "gatekeeping");

            migrationBuilder.DropColumn(
                name: "greet_delete_time",
                table: "gatekeeping");
        }
    }
}
