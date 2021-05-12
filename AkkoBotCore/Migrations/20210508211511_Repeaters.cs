using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

namespace AkkoBot.Migrations
{
    public partial class Repeaters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_absolute",
                table: "timers");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "time_of_day",
                table: "timers",
                type: "interval",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "repeaters",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    timer_id = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    author_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    interval = table.Column<TimeSpan>(type: "interval", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_repeaters", x => x.id);
                    table.ForeignKey(
                        name: "fk_repeaters_guild_config_guild_config_rel_id",
                        column: x => x.guild_id_fk,
                        principalTable: "guild_config",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stores repeater data and the context it should be sent to.");

            migrationBuilder.CreateIndex(
                name: "ix_repeaters_guild_id_fk",
                table: "repeaters",
                column: "guild_id_fk");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "repeaters");

            migrationBuilder.DropColumn(
                name: "time_of_day",
                table: "timers");

            migrationBuilder.AddColumn<bool>(
                name: "is_absolute",
                table: "timers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}