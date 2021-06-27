using Microsoft.EntityFrameworkCore.Migrations;
using System.Collections.Generic;

namespace AkkoBot.Migrations
{
    public partial class DelMsgOnCmd : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<long>>(
                name: "del_cmd_blacklist",
                table: "guild_config",
                type: "bigint[]",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "delete_cmd_on_message",
                table: "guild_config",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "del_cmd_blacklist",
                table: "guild_config");

            migrationBuilder.DropColumn(
                name: "delete_cmd_on_message",
                table: "guild_config");
        }
    }
}