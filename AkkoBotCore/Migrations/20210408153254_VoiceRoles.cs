using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace AkkoBot.Migrations
{
    public partial class VoiceRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "voice_roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    role_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_voice_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_voice_roles_guild_config_guild_config_rel_id",
                        column: x => x.guild_id_fk,
                        principalTable: "guild_config",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stores a voice chat role.");

            migrationBuilder.CreateIndex(
                name: "ix_voice_roles_guild_id_fk",
                table: "voice_roles",
                column: "guild_id_fk");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "voice_roles");
        }
    }
}
