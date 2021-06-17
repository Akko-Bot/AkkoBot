using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class BanTemplate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ban_template",
                table: "guild_config",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ban_template",
                table: "guild_config");
        }
    }
}