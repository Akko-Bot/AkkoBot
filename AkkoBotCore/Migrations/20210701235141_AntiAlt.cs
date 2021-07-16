using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace AkkoBot.Migrations
{
    public partial class AntiAlt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "anti_alt",
                table: "gatekeeping",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "anti_alt_punish_type",
                table: "gatekeeping",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "anti_alt_role_id",
                table: "gatekeeping",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "anti_alt_time",
                table: "gatekeeping",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "anti_alt",
                table: "gatekeeping");

            migrationBuilder.DropColumn(
                name: "anti_alt_punish_type",
                table: "gatekeeping");

            migrationBuilder.DropColumn(
                name: "anti_alt_role_id",
                table: "gatekeeping");

            migrationBuilder.DropColumn(
                name: "anti_alt_time",
                table: "gatekeeping");
        }
    }
}