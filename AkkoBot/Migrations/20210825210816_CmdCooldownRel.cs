using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class CmdCooldownRel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_command_cooldown_id",
                table: "command_cooldown");

            migrationBuilder.RenameColumn(
                name: "guild_id",
                table: "command_cooldown",
                newName: "guild_id_fk");

            migrationBuilder.CreateIndex(
                name: "ix_command_cooldown_guild_id_fk",
                table: "command_cooldown",
                column: "guild_id_fk");

            migrationBuilder.AddForeignKey(
                name: "fk_command_cooldown_guild_config_guild_config_rel_id",
                table: "command_cooldown",
                column: "guild_id_fk",
                principalTable: "guild_config",
                principalColumn: "guild_id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_command_cooldown_guild_config_guild_config_rel_id",
                table: "command_cooldown");

            migrationBuilder.DropIndex(
                name: "ix_command_cooldown_guild_id_fk",
                table: "command_cooldown");

            migrationBuilder.RenameColumn(
                name: "guild_id_fk",
                table: "command_cooldown",
                newName: "guild_id");

            migrationBuilder.CreateIndex(
                name: "ix_command_cooldown_id",
                table: "command_cooldown",
                column: "id");
        }
    }
}