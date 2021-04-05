using Microsoft.EntityFrameworkCore.Migrations;
using System.Collections.Generic;

namespace AkkoBot.Migrations
{
    public partial class CmdControl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "disabled_commands",
                table: "bot_config",
                type: "text[]",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "disabled_commands",
                table: "bot_config");
        }
    }
}