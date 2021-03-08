using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class UoW3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "type_id",
                table: "blacklist",
                newName: "context_id");

            migrationBuilder.RenameIndex(
                name: "ix_blacklist_type_id",
                table: "blacklist",
                newName: "ix_blacklist_context_id");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "blacklist",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "context_id",
                table: "blacklist",
                newName: "type_id");

            migrationBuilder.RenameIndex(
                name: "ix_blacklist_context_id",
                table: "blacklist",
                newName: "ix_blacklist_type_id");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "blacklist",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);
        }
    }
}