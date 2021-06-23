using Microsoft.EntityFrameworkCore.Migrations;
using System.Collections.Generic;

namespace AkkoBot.Migrations
{
    public partial class JoinRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<long>>(
                name: "join_roles",
                table: "guild_config",
                type: "bigint[]",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "join_roles",
                table: "guild_config");
        }
    }
}