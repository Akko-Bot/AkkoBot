using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class GuildLogIgnoredIds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<long>>(
                name: "guild_log_blacklist",
                table: "guild_config",
                type: "bigint[]",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "guild_log_blacklist",
                table: "guild_config");
        }
    }
}
