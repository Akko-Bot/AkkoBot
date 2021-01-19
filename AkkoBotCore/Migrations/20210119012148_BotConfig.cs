using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class BotConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "bot_id",
                table: "bot_config",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "pk_bot_config",
                table: "bot_config",
                column: "bot_id");

            migrationBuilder.CreateIndex(
                name: "ix_bot_config_bot_id",
                table: "bot_config",
                column: "bot_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_bot_config",
                table: "bot_config");

            migrationBuilder.DropIndex(
                name: "ix_bot_config_bot_id",
                table: "bot_config");

            migrationBuilder.DropColumn(
                name: "bot_id",
                table: "bot_config");
        }
    }
}
