using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class LogConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_bot_config_bot_id",
                table: "bot_config");

            migrationBuilder.DropColumn(
                name: "log_format",
                table: "bot_config");

            migrationBuilder.DropColumn(
                name: "log_time_format",
                table: "bot_config");

            migrationBuilder.CreateTable(
                name: "log_config_entity",
                columns: table => new
                {
                    bot_id_ref = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    log_format = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    log_time_format = table.Column<string>(type: "varchar", nullable: true),
                    is_logged_to_file = table.Column<bool>(type: "boolean", nullable: false),
                    log_size_mb = table.Column<double>(type: "double precision", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_log_config_entity", x => x.bot_id_ref);
                    table.ForeignKey(
                        name: "fk_log_config_entity_bot_config_bot_id_ref",
                        column: x => x.bot_id_ref,
                        principalTable: "bot_config",
                        principalColumn: "bot_id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "log_config_entity");

            migrationBuilder.AddColumn<string>(
                name: "log_format",
                table: "bot_config",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "log_time_format",
                table: "bot_config",
                type: "varchar",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_bot_config_bot_id",
                table: "bot_config",
                column: "bot_id");
        }
    }
}
