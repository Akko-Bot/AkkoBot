using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

namespace AkkoBot.Migrations
{
    public partial class MuteUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_guild_config",
                table: "guild_config");

            migrationBuilder.DropIndex(
                name: "ix_guild_config_guild_id",
                table: "guild_config");

            migrationBuilder.DropPrimaryKey(
                name: "pk_discord_users",
                table: "discord_users");

            migrationBuilder.DropIndex(
                name: "ix_discord_users_user_id",
                table: "discord_users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_blacklist",
                table: "blacklist");

            migrationBuilder.DropIndex(
                name: "ix_blacklist_context_id",
                table: "blacklist");

            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "playing_statuses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "guild_config",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "discord_users",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "blacklist",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_guild_config_guild_id",
                table: "guild_config",
                column: "guild_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_guild_config",
                table: "guild_config",
                column: "id");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_discord_users_user_id",
                table: "discord_users",
                column: "user_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_discord_users",
                table: "discord_users",
                column: "id");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_blacklist_context_id",
                table: "blacklist",
                column: "context_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_blacklist",
                table: "blacklist",
                column: "id");

            migrationBuilder.CreateTable(
                name: "muted_users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    elapses_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_muted_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_muted_users_guild_config_guild_config_rel_id",
                        column: x => x.guild_id_fk,
                        principalTable: "guild_config",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_muted_users_guild_id_fk",
                table: "muted_users",
                column: "guild_id_fk");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "muted_users");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_guild_config_guild_id",
                table: "guild_config");

            migrationBuilder.DropPrimaryKey(
                name: "pk_guild_config",
                table: "guild_config");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_discord_users_user_id",
                table: "discord_users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_discord_users",
                table: "discord_users");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_blacklist_context_id",
                table: "blacklist");

            migrationBuilder.DropPrimaryKey(
                name: "pk_blacklist",
                table: "blacklist");

            migrationBuilder.DropColumn(
                name: "id",
                table: "playing_statuses");

            migrationBuilder.DropColumn(
                name: "id",
                table: "guild_config");

            migrationBuilder.DropColumn(
                name: "id",
                table: "discord_users");

            migrationBuilder.DropColumn(
                name: "id",
                table: "blacklist");

            migrationBuilder.AddPrimaryKey(
                name: "pk_guild_config",
                table: "guild_config",
                column: "guild_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_discord_users",
                table: "discord_users",
                column: "user_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_blacklist",
                table: "blacklist",
                column: "context_id");

            migrationBuilder.CreateIndex(
                name: "ix_guild_config_guild_id",
                table: "guild_config",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "ix_discord_users_user_id",
                table: "discord_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_blacklist_context_id",
                table: "blacklist",
                column: "context_id");
        }
    }
}