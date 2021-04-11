using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class StickerFilter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_occurrencies_guild_config_guild_config_rel_id",
                table: "occurrencies");

            migrationBuilder.DropPrimaryKey(
                name: "pk_occurrencies",
                table: "occurrencies");

            migrationBuilder.RenameTable(
                name: "occurrencies",
                newName: "occurrences");

            migrationBuilder.RenameIndex(
                name: "ix_occurrencies_guild_id_fk",
                table: "occurrences",
                newName: "ix_occurrences_guild_id_fk");

            migrationBuilder.AddColumn<bool>(
                name: "filter_stickers",
                table: "filtered_words",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "pk_occurrences",
                table: "occurrences",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_occurrences_guild_config_guild_config_rel_id",
                table: "occurrences",
                column: "guild_id_fk",
                principalTable: "guild_config",
                principalColumn: "guild_id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_occurrences_guild_config_guild_config_rel_id",
                table: "occurrences");

            migrationBuilder.DropPrimaryKey(
                name: "pk_occurrences",
                table: "occurrences");

            migrationBuilder.DropColumn(
                name: "filter_stickers",
                table: "filtered_words");

            migrationBuilder.RenameTable(
                name: "occurrences",
                newName: "occurrencies");

            migrationBuilder.RenameIndex(
                name: "ix_occurrences_guild_id_fk",
                table: "occurrencies",
                newName: "ix_occurrencies_guild_id_fk");

            migrationBuilder.AddPrimaryKey(
                name: "pk_occurrencies",
                table: "occurrencies",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_occurrencies_guild_config_guild_config_rel_id",
                table: "occurrencies",
                column: "guild_id_fk",
                principalTable: "guild_config",
                principalColumn: "guild_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}