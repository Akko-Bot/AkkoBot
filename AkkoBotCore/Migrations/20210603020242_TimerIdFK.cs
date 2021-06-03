using Microsoft.EntityFrameworkCore.Migrations;

namespace AkkoBot.Migrations
{
    public partial class TimerIdFK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_reminders_id",
                table: "reminders");

            migrationBuilder.DropIndex(
                name: "ix_auto_commands_id",
                table: "auto_commands");

            migrationBuilder.DropColumn(
                name: "guild_id",
                table: "timers");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "warnings",
                newName: "user_id_fk");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "timers",
                newName: "guild_id_fk");

            migrationBuilder.RenameColumn(
                name: "timer_id",
                table: "repeaters",
                newName: "timer_id_fk");

            migrationBuilder.RenameColumn(
                name: "timer_id",
                table: "reminders",
                newName: "timer_id_fk");

            migrationBuilder.RenameColumn(
                name: "enabled",
                table: "filtered_words",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "timer_id",
                table: "auto_commands",
                newName: "timer_id_fk");

            migrationBuilder.AddColumn<int>(
                name: "timer_id_fk",
                table: "warnings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "timers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "user_id_fk",
                table: "timers",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "ix_warnings_timer_id_fk",
                table: "warnings",
                column: "timer_id_fk",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_warnings_user_id_fk",
                table: "warnings",
                column: "user_id_fk");

            migrationBuilder.CreateIndex(
                name: "ix_timers_guild_id_fk",
                table: "timers",
                column: "guild_id_fk");

            migrationBuilder.CreateIndex(
                name: "ix_timers_user_id_fk",
                table: "timers",
                column: "user_id_fk");

            migrationBuilder.CreateIndex(
                name: "ix_repeaters_timer_id_fk",
                table: "repeaters",
                column: "timer_id_fk",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reminders_timer_id_fk",
                table: "reminders",
                column: "timer_id_fk",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_playing_statuses_id",
                table: "playing_statuses",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_auto_commands_timer_id_fk",
                table: "auto_commands",
                column: "timer_id_fk",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_aliases_id",
                table: "aliases",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_auto_commands_timers_timer_rel_id",
                table: "auto_commands",
                column: "timer_id_fk",
                principalTable: "timers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_reminders_timers_timer_rel_id",
                table: "reminders",
                column: "timer_id_fk",
                principalTable: "timers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_repeaters_timers_timer_rel_id",
                table: "repeaters",
                column: "timer_id_fk",
                principalTable: "timers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_timers_discord_users_user_rel_id",
                table: "timers",
                column: "user_id_fk",
                principalTable: "discord_users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_timers_guild_config_guild_config_rel_id",
                table: "timers",
                column: "guild_id_fk",
                principalTable: "guild_config",
                principalColumn: "guild_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_warnings_discord_users_user_rel_id",
                table: "warnings",
                column: "user_id_fk",
                principalTable: "discord_users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_warnings_timers_timer_id_fk",
                table: "warnings",
                column: "timer_id_fk",
                principalTable: "timers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_auto_commands_timers_timer_rel_id",
                table: "auto_commands");

            migrationBuilder.DropForeignKey(
                name: "fk_reminders_timers_timer_rel_id",
                table: "reminders");

            migrationBuilder.DropForeignKey(
                name: "fk_repeaters_timers_timer_rel_id",
                table: "repeaters");

            migrationBuilder.DropForeignKey(
                name: "fk_timers_discord_users_user_rel_id",
                table: "timers");

            migrationBuilder.DropForeignKey(
                name: "fk_timers_guild_config_guild_config_rel_id",
                table: "timers");

            migrationBuilder.DropForeignKey(
                name: "fk_warnings_discord_users_user_rel_id",
                table: "warnings");

            migrationBuilder.DropForeignKey(
                name: "fk_warnings_timers_timer_id_fk",
                table: "warnings");

            migrationBuilder.DropIndex(
                name: "ix_warnings_timer_id_fk",
                table: "warnings");

            migrationBuilder.DropIndex(
                name: "ix_warnings_user_id_fk",
                table: "warnings");

            migrationBuilder.DropIndex(
                name: "ix_timers_guild_id_fk",
                table: "timers");

            migrationBuilder.DropIndex(
                name: "ix_timers_user_id_fk",
                table: "timers");

            migrationBuilder.DropIndex(
                name: "ix_repeaters_timer_id_fk",
                table: "repeaters");

            migrationBuilder.DropIndex(
                name: "ix_reminders_timer_id_fk",
                table: "reminders");

            migrationBuilder.DropIndex(
                name: "ix_playing_statuses_id",
                table: "playing_statuses");

            migrationBuilder.DropIndex(
                name: "ix_auto_commands_timer_id_fk",
                table: "auto_commands");

            migrationBuilder.DropIndex(
                name: "ix_aliases_id",
                table: "aliases");

            migrationBuilder.DropColumn(
                name: "timer_id_fk",
                table: "warnings");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "timers");

            migrationBuilder.DropColumn(
                name: "user_id_fk",
                table: "timers");

            migrationBuilder.RenameColumn(
                name: "user_id_fk",
                table: "warnings",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "guild_id_fk",
                table: "timers",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "timer_id_fk",
                table: "repeaters",
                newName: "timer_id");

            migrationBuilder.RenameColumn(
                name: "timer_id_fk",
                table: "reminders",
                newName: "timer_id");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "filtered_words",
                newName: "enabled");

            migrationBuilder.RenameColumn(
                name: "timer_id_fk",
                table: "auto_commands",
                newName: "timer_id");

            migrationBuilder.AddColumn<decimal>(
                name: "guild_id",
                table: "timers",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_reminders_id",
                table: "reminders",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_auto_commands_id",
                table: "auto_commands",
                column: "id");
        }
    }
}