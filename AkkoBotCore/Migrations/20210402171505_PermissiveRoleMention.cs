using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class PermissiveRoleMention : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "reminders",
                comment: "Stores reminder data and the context it should be sent to.");

            migrationBuilder.AddColumn<bool>(
                name: "permissive_role_mention",
                table: "guild_config",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "permissive_role_mention",
                table: "guild_config");

            migrationBuilder.AlterTable(
                name: "reminders",
                oldComment: "Stores reminder data and the context it should be sent to.");
        }
    }
}
