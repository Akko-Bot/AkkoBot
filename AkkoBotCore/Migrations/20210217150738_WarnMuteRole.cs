using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class WarnMuteRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "warn_punishments",
                comment: "Stores punishments to be automatically applied once a user reaches a certain amount of warnings.",
                oldComment: "Stores punishments to be automatically given once a user reaches a certain amount of warnings.");

            migrationBuilder.AlterColumn<decimal>(
                name: "punish_role_id",
                table: "warn_punishments",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "warn_punishments",
                comment: "Stores punishments to be automatically given once a user reaches a certain amount of warnings.",
                oldComment: "Stores punishments to be automatically applied once a user reaches a certain amount of warnings.");

            migrationBuilder.AlterColumn<decimal>(
                name: "punish_role_id",
                table: "warn_punishments",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);
        }
    }
}