using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

namespace AkkoBot.Migrations
{
    public partial class BlReasonAndTimers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_log_configs",
                table: "log_configs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_guild_configs",
                table: "guild_configs");

            migrationBuilder.RenameTable(
                name: "log_configs",
                newName: "log_config");

            migrationBuilder.RenameTable(
                name: "guild_configs",
                newName: "guild_config");

            migrationBuilder.RenameIndex(
                name: "ix_guild_configs_guild_id",
                table: "guild_config",
                newName: "ix_guild_config_guild_id");

            migrationBuilder.AlterColumn<string>(
                name: "locale",
                table: "bot_config",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(6)",
                oldMaxLength: 6);

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "bot_config",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "reason",
                table: "blacklist",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "log_config",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "locale",
                table: "guild_config",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(6)",
                oldMaxLength: 6);

            migrationBuilder.AddPrimaryKey(
                name: "pk_log_config",
                table: "log_config",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_guild_config",
                table: "guild_config",
                column: "guild_id");

            migrationBuilder.CreateTable(
                name: "timers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    interval = table.Column<TimeSpan>(type: "interval", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    elapse_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_timers", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "timers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_log_config",
                table: "log_config");

            migrationBuilder.DropPrimaryKey(
                name: "pk_guild_config",
                table: "guild_config");

            migrationBuilder.DropColumn(
                name: "reason",
                table: "blacklist");

            migrationBuilder.RenameTable(
                name: "log_config",
                newName: "log_configs");

            migrationBuilder.RenameTable(
                name: "guild_config",
                newName: "guild_configs");

            migrationBuilder.RenameIndex(
                name: "ix_guild_config_guild_id",
                table: "guild_configs",
                newName: "ix_guild_configs_guild_id");

            migrationBuilder.AlterColumn<string>(
                name: "locale",
                table: "bot_config",
                type: "varchar(6)",
                maxLength: 6,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "bot_config",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "log_configs",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "locale",
                table: "guild_configs",
                type: "varchar(6)",
                maxLength: 6,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AddPrimaryKey(
                name: "pk_log_configs",
                table: "log_configs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_guild_configs",
                table: "guild_configs",
                column: "guild_id");
        }
    }
}