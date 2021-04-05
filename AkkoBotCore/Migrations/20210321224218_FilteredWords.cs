using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;
using System.Collections.Generic;

namespace AkkoBot.Migrations
{
    public partial class FilteredWords : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filtered_words",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    words = table.Column<List<string>>(type: "text[]", nullable: true),
                    ignored_ids = table.Column<List<long>>(type: "bigint[]", nullable: true),
                    notification_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    notify_on_delete = table.Column<bool>(type: "boolean", nullable: false),
                    warn_on_delete = table.Column<bool>(type: "boolean", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filtered_words", x => x.id);
                    table.ForeignKey(
                        name: "fk_filtered_words_guild_config_guild_config_rel_id",
                        column: x => x.guild_id_fk,
                        principalTable: "guild_config",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stores filtered words of a Discord server.");

            migrationBuilder.CreateIndex(
                name: "ix_filtered_words_guild_id_fk",
                table: "filtered_words",
                column: "guild_id_fk",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filtered_words");
        }
    }
}