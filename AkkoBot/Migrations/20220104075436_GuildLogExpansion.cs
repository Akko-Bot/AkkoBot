using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkkoBot.Migrations;

public partial class GuildLogExpansion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<long>(
            name: "type",
            table: "guild_logs",
            type: "bigint",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "type",
            table: "guild_logs",
            type: "integer",
            nullable: false,
            oldClrType: typeof(long),
            oldType: "bigint");
    }
}