using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

namespace AkkoBot.Migrations
{
    public partial class CommandCooldown : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "command_cooldown",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    command = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    cooldown = table.Column<TimeSpan>(type: "interval", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_command_cooldown", x => x.id);
                },
                comment: "Stores commands whose execution is restricted by a cooldown.");

            migrationBuilder.CreateIndex(
                name: "ix_command_cooldown_id",
                table: "command_cooldown",
                column: "id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "command_cooldown");
        }
    }
}