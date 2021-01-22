using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace AkkoBot.Migrations
{
    public partial class LogConfigKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "log_configs",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "bot_config",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "pk_log_configs",
                table: "log_configs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_bot_config",
                table: "bot_config",
                column: "id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_log_configs",
                table: "log_configs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_bot_config",
                table: "bot_config");

            migrationBuilder.DropColumn(
                name: "id",
                table: "log_configs");

            migrationBuilder.DropColumn(
                name: "id",
                table: "bot_config");
        }
    }
}
