using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;
using System.Collections.Generic;

#nullable disable

namespace AkkoBot.Migrations;

public partial class Modroles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "just_a_test",
            table: "muted_users");

        migrationBuilder.AlterColumn<List<long>>(
            name: "ignored_ids",
            table: "tags",
            type: "bigint[]",
            nullable: false,
            oldClrType: typeof(List<long>),
            oldType: "bigint[]",
            oldNullable: true);

        migrationBuilder.AlterColumn<int[]>(
            name: "votes",
            table: "polls",
            type: "integer[]",
            nullable: false,
            defaultValue: new int[0],
            oldClrType: typeof(int[]),
            oldType: "integer[]",
            oldNullable: true);

        migrationBuilder.AlterColumn<List<long>>(
            name: "voters",
            table: "polls",
            type: "bigint[]",
            nullable: false,
            oldClrType: typeof(List<long>),
            oldType: "bigint[]",
            oldNullable: true);

        migrationBuilder.AlterColumn<string[]>(
            name: "answers",
            table: "polls",
            type: "text[]",
            nullable: false,
            defaultValue: new string[0],
            oldClrType: typeof(string[]),
            oldType: "text[]",
            oldNullable: true);

        migrationBuilder.AlterColumn<List<long>>(
            name: "allowed_user_ids",
            table: "permission_override",
            type: "bigint[]",
            nullable: false,
            oldClrType: typeof(List<long>),
            oldType: "bigint[]",
            oldNullable: true);

        migrationBuilder.AlterColumn<List<long>>(
            name: "allowed_role_ids",
            table: "permission_override",
            type: "bigint[]",
            nullable: false,
            oldClrType: typeof(List<long>),
            oldType: "bigint[]",
            oldNullable: true);

        migrationBuilder.AlterColumn<List<long>>(
            name: "allowed_channel_ids",
            table: "permission_override",
            type: "bigint[]",
            nullable: false,
            oldClrType: typeof(List<long>),
            oldType: "bigint[]",
            oldNullable: true);

        migrationBuilder.AlterColumn<List<long>>(
            name: "join_roles",
            table: "guild_config",
            type: "bigint[]",
            nullable: false,
            oldClrType: typeof(List<long>),
            oldType: "bigint[]",
            oldNullable: true);

        migrationBuilder.AlterColumn<List<long>>(
            name: "guild_log_blacklist",
            table: "guild_config",
            type: "bigint[]",
            nullable: false,
            oldClrType: typeof(List<long>),
            oldType: "bigint[]",
            oldNullable: true);

        migrationBuilder.AlterColumn<List<long>>(
            name: "del_cmd_blacklist",
            table: "guild_config",
            type: "bigint[]",
            nullable: false,
            oldClrType: typeof(List<long>),
            oldType: "bigint[]",
            oldNullable: true);

        migrationBuilder.AlterColumn<List<string>>(
            name: "words",
            table: "filtered_words",
            type: "text[]",
            nullable: false,
            oldClrType: typeof(List<string>),
            oldType: "text[]",
            oldNullable: true);

        migrationBuilder.AlterColumn<List<long>>(
            name: "ignored_ids",
            table: "filtered_words",
            type: "bigint[]",
            nullable: false,
            oldClrType: typeof(List<long>),
            oldType: "bigint[]",
            oldNullable: true);

        migrationBuilder.AlterColumn<List<long>>(
            name: "ignored_ids",
            table: "auto_slowmode",
            type: "bigint[]",
            nullable: false,
            oldClrType: typeof(List<long>),
            oldType: "bigint[]",
            oldNullable: true);

        migrationBuilder.CreateTable(
            name: "modroles",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                modrole_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                behavior = table.Column<int>(type: "integer", nullable: false),
                target_role_ids = table.Column<List<long>>(type: "bigint[]", nullable: false),
                date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_modroles", x => x.id);
                table.ForeignKey(
                    name: "fk_modroles_guild_config_guild_config_rel_id",
                    column: x => x.guild_id_fk,
                    principalTable: "guild_config",
                    principalColumn: "guild_id",
                    onDelete: ReferentialAction.Cascade);
            },
            comment: "Stores modrole data.");

        migrationBuilder.CreateIndex(
            name: "ix_modroles_guild_id_fk",
            table: "modroles",
            column: "guild_id_fk");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "modroles");

        migrationBuilder.AlterColumn<List<long>>(
            name: "ignored_ids",
            table: "tags",
            type: "bigint[]",
            nullable: true,
            oldClrType: typeof(List<long>),
            oldType: "bigint[]");

        migrationBuilder.AlterColumn<int[]>(
            name: "votes",
            table: "polls",
            type: "integer[]",
            nullable: true,
            oldClrType: typeof(int[]),
            oldType: "integer[]");

        migrationBuilder.AlterColumn<List<long>>(
            name: "voters",
            table: "polls",
            type: "bigint[]",
            nullable: true,
            oldClrType: typeof(List<long>),
            oldType: "bigint[]");

        migrationBuilder.AlterColumn<string[]>(
            name: "answers",
            table: "polls",
            type: "text[]",
            nullable: true,
            oldClrType: typeof(string[]),
            oldType: "text[]");

        migrationBuilder.AlterColumn<List<long>>(
            name: "allowed_user_ids",
            table: "permission_override",
            type: "bigint[]",
            nullable: true,
            oldClrType: typeof(List<long>),
            oldType: "bigint[]");

        migrationBuilder.AlterColumn<List<long>>(
            name: "allowed_role_ids",
            table: "permission_override",
            type: "bigint[]",
            nullable: true,
            oldClrType: typeof(List<long>),
            oldType: "bigint[]");

        migrationBuilder.AlterColumn<List<long>>(
            name: "allowed_channel_ids",
            table: "permission_override",
            type: "bigint[]",
            nullable: true,
            oldClrType: typeof(List<long>),
            oldType: "bigint[]");

        migrationBuilder.AddColumn<string>(
            name: "just_a_test",
            table: "muted_users",
            type: "text",
            nullable: true);

        migrationBuilder.AlterColumn<List<long>>(
            name: "join_roles",
            table: "guild_config",
            type: "bigint[]",
            nullable: true,
            oldClrType: typeof(List<long>),
            oldType: "bigint[]");

        migrationBuilder.AlterColumn<List<long>>(
            name: "guild_log_blacklist",
            table: "guild_config",
            type: "bigint[]",
            nullable: true,
            oldClrType: typeof(List<long>),
            oldType: "bigint[]");

        migrationBuilder.AlterColumn<List<long>>(
            name: "del_cmd_blacklist",
            table: "guild_config",
            type: "bigint[]",
            nullable: true,
            oldClrType: typeof(List<long>),
            oldType: "bigint[]");

        migrationBuilder.AlterColumn<List<string>>(
            name: "words",
            table: "filtered_words",
            type: "text[]",
            nullable: true,
            oldClrType: typeof(List<string>),
            oldType: "text[]");

        migrationBuilder.AlterColumn<List<long>>(
            name: "ignored_ids",
            table: "filtered_words",
            type: "bigint[]",
            nullable: true,
            oldClrType: typeof(List<long>),
            oldType: "bigint[]");

        migrationBuilder.AlterColumn<List<long>>(
            name: "ignored_ids",
            table: "auto_slowmode",
            type: "bigint[]",
            nullable: true,
            oldClrType: typeof(List<long>),
            oldType: "bigint[]");
    }
}