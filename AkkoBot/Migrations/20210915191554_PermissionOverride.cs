using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;
using System.Collections.Generic;

namespace AkkoBot.Migrations
{
    public partial class PermissionOverride : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "permission_override",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    allowed_user_ids = table.Column<List<long>>(type: "bigint[]", nullable: true),
                    allowed_channel_ids = table.Column<List<long>>(type: "bigint[]", nullable: true),
                    allowed_role_ids = table.Column<List<long>>(type: "bigint[]", nullable: true),
                    guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    command = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    permissions = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permission_override", x => x.id);
                    table.ForeignKey(
                        name: "fk_permission_override_guild_config_guild_config_rel_id",
                        column: x => x.guild_id_fk,
                        principalTable: "guild_config",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stores data related to permission overrides for commands.");

            migrationBuilder.CreateIndex(
                name: "ix_permission_override_guild_id_fk",
                table: "permission_override",
                column: "guild_id_fk");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "permission_override");
        }
    }
}