using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class AliasRel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_aliases_id",
                table: "aliases");

            migrationBuilder.RenameColumn(
                name: "guild_id",
                table: "aliases",
                newName: "guild_id_fk");

            migrationBuilder.CreateIndex(
                name: "ix_aliases_guild_id_fk",
                table: "aliases",
                column: "guild_id_fk");

            migrationBuilder.AddForeignKey(
                name: "fk_aliases_guild_config_guild_config_rel_id",
                table: "aliases",
                column: "guild_id_fk",
                principalTable: "guild_config",
                principalColumn: "guild_id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_aliases_guild_config_guild_config_rel_id",
                table: "aliases");

            migrationBuilder.DropIndex(
                name: "ix_aliases_guild_id_fk",
                table: "aliases");

            migrationBuilder.RenameColumn(
                name: "guild_id_fk",
                table: "aliases",
                newName: "guild_id");

            migrationBuilder.CreateIndex(
                name: "ix_aliases_id",
                table: "aliases",
                column: "id");
        }
    }
}
