using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class FcStickerAdd : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_attachment_only",
                table: "filtered_content");

            migrationBuilder.DropColumn(
                name: "is_command_only",
                table: "filtered_content");

            migrationBuilder.DropColumn(
                name: "is_image_only",
                table: "filtered_content");

            migrationBuilder.DropColumn(
                name: "is_invite_only",
                table: "filtered_content");

            migrationBuilder.DropColumn(
                name: "is_url_only",
                table: "filtered_content");

            migrationBuilder.AddColumn<int>(
                name: "content_type",
                table: "filtered_content",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "content_type",
                table: "filtered_content");

            migrationBuilder.AddColumn<bool>(
                name: "is_attachment_only",
                table: "filtered_content",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_command_only",
                table: "filtered_content",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_image_only",
                table: "filtered_content",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_invite_only",
                table: "filtered_content",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_url_only",
                table: "filtered_content",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}