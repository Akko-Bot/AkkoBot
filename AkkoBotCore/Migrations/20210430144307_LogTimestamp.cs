using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class LogTimestamp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "filtered_content",
                comment: "Stores the content filters to be applied to a Discord channel.",
                oldComment: "Stores the kind of content filter of a Discord channel.");

            migrationBuilder.AddColumn<string>(
                name: "log_time_stamp",
                table: "log_config",
                type: "varchar",
                maxLength: 20,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "log_time_stamp",
                table: "log_config");

            migrationBuilder.AlterTable(
                name: "filtered_content",
                comment: "Stores the kind of content filter of a Discord channel.",
                oldComment: "Stores the content filters to be applied to a Discord channel.");
        }
    }
}
