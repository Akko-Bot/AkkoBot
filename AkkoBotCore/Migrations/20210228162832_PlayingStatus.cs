using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class PlayingStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "stream_url",
                table: "playing_statuses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "rotate_status",
                table: "bot_config",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "stream_url",
                table: "playing_statuses");

            migrationBuilder.DropColumn(
                name: "rotate_status",
                table: "bot_config");
        }
    }
}
