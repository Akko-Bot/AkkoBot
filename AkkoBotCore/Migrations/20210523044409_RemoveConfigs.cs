using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace AkkoBot.Migrations
{
    public partial class RemoveConfigs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bot_config");

            migrationBuilder.DropTable(
                name: "log_config");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bot_config",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bot_prefix = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    case_sensitive_commands = table.Column<bool>(type: "boolean", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    disabled_commands = table.Column<string[]>(type: "text[]", nullable: true),
                    enable_help = table.Column<bool>(type: "boolean", nullable: false),
                    error_color = table.Column<string>(type: "varchar(6)", maxLength: 6, nullable: false),
                    interactive_timeout = table.Column<TimeSpan>(type: "interval", nullable: false),
                    locale = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    mention_prefix = table.Column<bool>(type: "boolean", nullable: false),
                    message_size_cache = table.Column<int>(type: "integer", nullable: false),
                    min_warn_expire = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ok_color = table.Column<string>(type: "varchar(6)", maxLength: 6, nullable: false),
                    respond_to_dms = table.Column<bool>(type: "boolean", nullable: false),
                    rotate_status = table.Column<bool>(type: "boolean", nullable: false),
                    use_embed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bot_config", x => x.id);
                },
                comment: "Stores settings related to the bot.");

            migrationBuilder.CreateTable(
                name: "log_config",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_logged_to_file = table.Column<bool>(type: "boolean", nullable: false),
                    log_format = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    log_level = table.Column<int>(type: "integer", nullable: false),
                    log_size_mb = table.Column<double>(type: "double precision", nullable: false),
                    log_time_format = table.Column<string>(type: "varchar", maxLength: 20, nullable: true),
                    log_time_stamp = table.Column<string>(type: "varchar", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_log_config", x => x.id);
                },
                comment: "Stores data and settings related to how the bot logs command usage.");
        }
    }
}
