using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class RenameWarns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_warn_entity_guild_config_guild_config_rel_id",
                table: "warn_entity");

            migrationBuilder.DropForeignKey(
                name: "fk_warn_punish_entity_guild_config_guild_config_rel_id",
                table: "warn_punish_entity");

            migrationBuilder.DropPrimaryKey(
                name: "pk_warn_punish_entity",
                table: "warn_punish_entity");

            migrationBuilder.DropPrimaryKey(
                name: "pk_warn_entity",
                table: "warn_entity");

            migrationBuilder.RenameTable(
                name: "warn_punish_entity",
                newName: "warn_punishments");

            migrationBuilder.RenameTable(
                name: "warn_entity",
                newName: "warnings");

            migrationBuilder.RenameIndex(
                name: "ix_warn_punish_entity_guild_id_fk",
                table: "warn_punishments",
                newName: "ix_warn_punishments_guild_id_fk");

            migrationBuilder.RenameIndex(
                name: "ix_warn_entity_guild_id_fk",
                table: "warnings",
                newName: "ix_warnings_guild_id_fk");

            migrationBuilder.AddPrimaryKey(
                name: "pk_warn_punishments",
                table: "warn_punishments",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_warnings",
                table: "warnings",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_warn_punishments_guild_config_guild_config_rel_id",
                table: "warn_punishments",
                column: "guild_id_fk",
                principalTable: "guild_config",
                principalColumn: "guild_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_warnings_guild_config_guild_config_rel_id",
                table: "warnings",
                column: "guild_id_fk",
                principalTable: "guild_config",
                principalColumn: "guild_id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_warn_punishments_guild_config_guild_config_rel_id",
                table: "warn_punishments");

            migrationBuilder.DropForeignKey(
                name: "fk_warnings_guild_config_guild_config_rel_id",
                table: "warnings");

            migrationBuilder.DropPrimaryKey(
                name: "pk_warnings",
                table: "warnings");

            migrationBuilder.DropPrimaryKey(
                name: "pk_warn_punishments",
                table: "warn_punishments");

            migrationBuilder.RenameTable(
                name: "warnings",
                newName: "warn_entity");

            migrationBuilder.RenameTable(
                name: "warn_punishments",
                newName: "warn_punish_entity");

            migrationBuilder.RenameIndex(
                name: "ix_warnings_guild_id_fk",
                table: "warn_entity",
                newName: "ix_warn_entity_guild_id_fk");

            migrationBuilder.RenameIndex(
                name: "ix_warn_punishments_guild_id_fk",
                table: "warn_punish_entity",
                newName: "ix_warn_punish_entity_guild_id_fk");

            migrationBuilder.AddPrimaryKey(
                name: "pk_warn_entity",
                table: "warn_entity",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_warn_punish_entity",
                table: "warn_punish_entity",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_warn_entity_guild_config_guild_config_rel_id",
                table: "warn_entity",
                column: "guild_id_fk",
                principalTable: "guild_config",
                principalColumn: "guild_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_warn_punish_entity_guild_config_guild_config_rel_id",
                table: "warn_punish_entity",
                column: "guild_id_fk",
                principalTable: "guild_config",
                principalColumn: "guild_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}