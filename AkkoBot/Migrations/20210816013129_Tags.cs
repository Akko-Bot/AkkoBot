using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;
using System.Collections.Generic;

namespace AkkoBot.Migrations
{
    public partial class Tags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ignore_global_tags",
                table: "guild_config",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "minimum_tag_permissions",
                table: "guild_config",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ignored_ids = table.Column<List<long>>(type: "bigint[]", nullable: true),
                    guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    author_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    trigger = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    response = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    is_emoji = table.Column<bool>(type: "boolean", nullable: false),
                    behavior = table.Column<int>(type: "integer", nullable: false),
                    allowed_perms = table.Column<long>(type: "bigint", nullable: false),
                    last_day_used = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => x.id);
                    table.ForeignKey(
                        name: "fk_tags_guild_config_guild_config_rel_id",
                        column: x => x.guild_id_fk,
                        principalTable: "guild_config",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stores data related to a tag.");

            migrationBuilder.CreateIndex(
                name: "ix_tags_guild_id_fk",
                table: "tags",
                column: "guild_id_fk");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropColumn(
                name: "ignore_global_tags",
                table: "guild_config");

            migrationBuilder.DropColumn(
                name: "minimum_tag_permissions",
                table: "guild_config");
        }
    }
}