using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace AkkoBot.Migrations
{
    public partial class AutoSlowmode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auto_slowmode",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ignored_ids = table.Column<List<long>>(type: "bigint[]", nullable: true),
                    guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    message_amount = table.Column<int>(type: "integer", nullable: false),
                    slowmode_trigger_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    slowmode_duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    slowmode_interval = table.Column<TimeSpan>(type: "interval", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auto_slowmode", x => x.id);
                    table.ForeignKey(
                        name: "fk_auto_slowmode_guild_config_guild_config_rel_id",
                        column: x => x.guild_id_fk,
                        principalTable: "guild_config",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stores the settings for the automatic slow mode of a Discord server.");

            migrationBuilder.CreateIndex(
                name: "ix_auto_slowmode_guild_id_fk",
                table: "auto_slowmode",
                column: "guild_id_fk",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auto_slowmode");
        }
    }
}
