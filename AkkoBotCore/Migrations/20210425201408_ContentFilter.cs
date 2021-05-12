using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

namespace AkkoBot.Migrations
{
    public partial class ContentFilter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filtered_content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    is_attachment_only = table.Column<bool>(type: "boolean", nullable: false),
                    is_image_only = table.Column<bool>(type: "boolean", nullable: false),
                    is_url_only = table.Column<bool>(type: "boolean", nullable: false),
                    is_invite_only = table.Column<bool>(type: "boolean", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filtered_content", x => x.id);
                    table.ForeignKey(
                        name: "fk_filtered_content_guild_config_guild_config_rel_id",
                        column: x => x.guild_id_fk,
                        principalTable: "guild_config",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stores the kind of content filter of a Discord channel.");

            migrationBuilder.CreateIndex(
                name: "ix_filtered_content_guild_id_fk",
                table: "filtered_content",
                column: "guild_id_fk");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filtered_content");
        }
    }
}