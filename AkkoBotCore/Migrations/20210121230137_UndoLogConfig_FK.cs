using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class UndoLogConfig_FK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_log_config_entity_bot_config_bot_id_ref",
                table: "log_config_entity");

            migrationBuilder.DropPrimaryKey(
                name: "pk_bot_config",
                table: "bot_config");

            migrationBuilder.DropPrimaryKey(
                name: "pk_log_config_entity",
                table: "log_config_entity");

            migrationBuilder.DropColumn(
                name: "bot_id",
                table: "bot_config");

            migrationBuilder.DropColumn(
                name: "bot_id_ref",
                table: "log_config_entity");

            migrationBuilder.RenameTable(
                name: "log_config_entity",
                newName: "log_configs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "log_configs",
                newName: "log_config_entity");

            migrationBuilder.AddColumn<decimal>(
                name: "bot_id",
                table: "bot_config",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "bot_id_ref",
                table: "log_config_entity",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "pk_bot_config",
                table: "bot_config",
                column: "bot_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_log_config_entity",
                table: "log_config_entity",
                column: "bot_id_ref");

            migrationBuilder.AddForeignKey(
                name: "fk_log_config_entity_bot_config_bot_id_ref",
                table: "log_config_entity",
                column: "bot_id_ref",
                principalTable: "bot_config",
                principalColumn: "bot_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
