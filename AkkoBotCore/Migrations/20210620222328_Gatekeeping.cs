using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

namespace AkkoBot.Migrations
{
    public partial class Gatekeeping : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "custom_sanitized_name",
                table: "guild_config");

            migrationBuilder.DropColumn(
                name: "sanitize_names",
                table: "guild_config");

            migrationBuilder.CreateTable(
                name: "gatekeeping",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    sanitize_names = table.Column<bool>(type: "boolean", nullable: false),
                    custom_sanitized_name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    greet_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    farewell_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    greet_channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    farewell_channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    greet_dm = table.Column<bool>(type: "boolean", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gatekeeping", x => x.id);
                    table.ForeignKey(
                        name: "fk_gatekeeping_guild_config_guild_config_rel_id",
                        column: x => x.guild_id_fk,
                        principalTable: "guild_config",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stores settings and data related to gatekeeping.");

            migrationBuilder.CreateIndex(
                name: "ix_gatekeeping_guild_id_fk",
                table: "gatekeeping",
                column: "guild_id_fk",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gatekeeping");

            migrationBuilder.AddColumn<string>(
                name: "custom_sanitized_name",
                table: "guild_config",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "sanitize_names",
                table: "guild_config",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}