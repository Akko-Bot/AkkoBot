using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class UoW_1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "blacklist",
                columns: table => new
                {
                    type_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blacklist", x => x.type_id);
                },
                comment: "Stores users, channels, and servers blacklisted from the bot.");

            migrationBuilder.CreateTable(
                name: "bot_config",
                columns: table => new
                {
                    default_prefix = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    log_format = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    log_time_format = table.Column<string>(type: "varchar", nullable: true),
                    respond_to_dms = table.Column<bool>(type: "boolean", nullable: false),
                    case_sensitive_commands = table.Column<bool>(type: "boolean", nullable: false),
                    message_size_cache = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                },
                comment: "Stores settings related to the bot.");

            migrationBuilder.CreateTable(
                name: "discord_users",
                columns: table => new
                {
                    user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    username = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    discriminator = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discord_users", x => x.user_id);
                },
                comment: "Stores data and settings related to individual Discord users.");

            migrationBuilder.CreateTable(
                name: "guild_configs",
                columns: table => new
                {
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    prefix = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    use_embed = table.Column<bool>(type: "boolean", nullable: false),
                    ok_color = table.Column<string>(type: "varchar(6)", maxLength: 6, nullable: false),
                    error_color = table.Column<string>(type: "varchar(6)", maxLength: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guild_configs", x => x.guild_id);
                },
                comment: "Stores settings related to individual Discord servers.");

            migrationBuilder.CreateTable(
                name: "playing_statuses",
                columns: table => new
                {
                    message = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    rotation_time = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                },
                comment: "Stores data related to the bot's Discord status.");

            migrationBuilder.CreateIndex(
                name: "ix_blacklist_type_id",
                table: "blacklist",
                column: "type_id");

            migrationBuilder.CreateIndex(
                name: "ix_discord_users_user_id",
                table: "discord_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_guild_configs_guild_id",
                table: "guild_configs",
                column: "guild_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blacklist");

            migrationBuilder.DropTable(
                name: "bot_config");

            migrationBuilder.DropTable(
                name: "discord_users");

            migrationBuilder.DropTable(
                name: "guild_configs");

            migrationBuilder.DropTable(
                name: "playing_statuses");
        }
    }
}
