using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;
using System.Collections.Generic;

namespace AkkoBot.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "aliases",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    is_dynamic = table.Column<bool>(type: "boolean", nullable: false),
                    alias = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    command = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    arguments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_aliases", x => x.id);
                },
                comment: "Stores command aliases.");

            migrationBuilder.CreateTable(
                name: "blacklist",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    context_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(37)", maxLength: 37, nullable: true),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blacklist", x => x.id);
                    table.UniqueConstraint("ak_blacklist_context_id", x => x.context_id);
                },
                comment: "Stores users, channels, and servers blacklisted from the bot.");

            migrationBuilder.CreateTable(
                name: "command_cooldown",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    command = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    cooldown = table.Column<TimeSpan>(type: "interval", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_command_cooldown", x => x.id);
                },
                comment: "Stores commands whose execution is restricted by a cooldown.");

            migrationBuilder.CreateTable(
                name: "discord_users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    username = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    discriminator = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discord_users", x => x.id);
                    table.UniqueConstraint("ak_discord_users_user_id", x => x.user_id);
                },
                comment: "Stores data related to individual Discord users.");

            migrationBuilder.CreateTable(
                name: "guild_config",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    join_roles = table.Column<List<long>>(type: "bigint[]", nullable: true),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    prefix = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    locale = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    ok_color = table.Column<string>(type: "varchar(6)", maxLength: 6, nullable: false),
                    error_color = table.Column<string>(type: "varchar(6)", maxLength: 6, nullable: false),
                    ban_template = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    timezone = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    use_embed = table.Column<bool>(type: "boolean", nullable: false),
                    permissive_role_mention = table.Column<bool>(type: "boolean", nullable: false),
                    mute_role_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    warn_expire = table.Column<TimeSpan>(type: "interval", nullable: false),
                    interactive_timeout = table.Column<TimeSpan>(type: "interval", nullable: true),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guild_config", x => x.id);
                    table.UniqueConstraint("ak_guild_config_guild_id", x => x.guild_id);
                },
                comment: "Stores settings and data related to a Discord server.");

            migrationBuilder.CreateTable(
                name: "playing_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    message = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    stream_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    rotation_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_playing_statuses", x => x.id);
                },
                comment: "Stores data related to the bot's Discord status.");

            migrationBuilder.CreateTable(
                name: "filtered_content",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    is_attachment_only = table.Column<bool>(type: "boolean", nullable: false),
                    is_image_only = table.Column<bool>(type: "boolean", nullable: false),
                    is_url_only = table.Column<bool>(type: "boolean", nullable: false),
                    is_invite_only = table.Column<bool>(type: "boolean", nullable: false),
                    is_command_only = table.Column<bool>(type: "boolean", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filtered_content", x => x.id);
                    table.ForeignKey(
                        name: "fk_filtered_content_guild_config_guild_config_rel_id",
                        column: x => x.guild_id_fk,
                        principalTable: "guild_config",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stores the content filters to be applied to a Discord channel.");

            migrationBuilder.CreateTable(
                name: "filtered_words",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    words = table.Column<List<string>>(type: "text[]", nullable: true),
                    ignored_ids = table.Column<List<long>>(type: "bigint[]", nullable: true),
                    guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    notification_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    filter_stickers = table.Column<bool>(type: "boolean", nullable: false),
                    filter_invites = table.Column<bool>(type: "boolean", nullable: false),
                    notify_on_delete = table.Column<bool>(type: "boolean", nullable: false),
                    warn_on_delete = table.Column<bool>(type: "boolean", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filtered_words", x => x.id);
                    table.ForeignKey(
                        name: "fk_filtered_words_guild_config_guild_config_rel_id",
                        column: x => x.guild_id_fk,
                        principalTable: "guild_config",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stores filtered words of a Discord server.");

            migrationBuilder.CreateTable(
                name: "gatekeeping",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    sanitize_names = table.Column<bool>(type: "boolean", nullable: false),
                    custom_sanitized_name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    greet_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    farewell_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    greet_channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    farewell_channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    greet_dm = table.Column<bool>(type: "boolean", nullable: false),
                    greet_delete_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    farewell_delete_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gatekeeping", x => x.id);
                    table.ForeignKey(
                        name: "fk_gatekeeping_guild_config_guild_config_rel_id",
                        column: x => x.guild_id_fk,
                        principalTable: "guild_config",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stores settings and data related to gatekeeping.");

            migrationBuilder.CreateTable(
                name: "muted_users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_muted_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_muted_users_guild_config_guild_config_rel_id",
                        column: x => x.guild_id_fk,
                        principalTable: "guild_config",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stores data about users that got muted in a specific server.");

            migrationBuilder.CreateTable(
                name: "occurrences",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    notices = table.Column<int>(type: "integer", nullable: false),
                    warnings = table.Column<int>(type: "integer", nullable: false),
                    mutes = table.Column<int>(type: "integer", nullable: false),
                    kicks = table.Column<int>(type: "integer", nullable: false),
                    softbans = table.Column<int>(type: "integer", nullable: false),
                    bans = table.Column<int>(type: "integer", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_occurrences", x => x.id);
                    table.ForeignKey(
                        name: "fk_occurrences_guild_config_guild_config_rel_id",
                        column: x => x.guild_id_fk,
                        principalTable: "guild_config",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stores the amount of infractions commited by a user in a server.");

            migrationBuilder.CreateTable(
                name: "polls",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    message_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    question = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    answers = table.Column<string[]>(type: "text[]", nullable: true),
                    votes = table.Column<int[]>(type: "integer[]", nullable: true),
                    voters = table.Column<List<long>>(type: "bigint[]", nullable: true),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_polls", x => x.id);
                    table.ForeignKey(
                        name: "fk_polls_guild_config_guild_config_rel_id",
                        column: x => x.guild_id_fk,
                        principalTable: "guild_config",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stores data related to a server poll.");

            migrationBuilder.CreateTable(
                name: "timers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    role_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    is_repeatable = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    interval = table.Column<TimeSpan>(type: "interval", nullable: false),
                    time_of_day = table.Column<TimeSpan>(type: "interval", nullable: true),
                    elapse_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_timers", x => x.id);
                    table.ForeignKey(
                        name: "fk_timers_discord_users_user_rel_id",
                        column: x => x.user_id_fk,
                        principalTable: "discord_users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_timers_guild_config_guild_config_rel_id",
                        column: x => x.guild_id_fk,
                        principalTable: "guild_config",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stores a timer that executes actions at some point in the future.");

            migrationBuilder.CreateTable(
                name: "voice_roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    role_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_voice_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_voice_roles_guild_config_guild_config_rel_id",
                        column: x => x.guild_id_fk,
                        principalTable: "guild_config",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stores a voice chat role.");

            migrationBuilder.CreateTable(
                name: "warn_punishments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    warn_amount = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    interval = table.Column<TimeSpan>(type: "interval", nullable: true),
                    punish_role_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warn_punishments", x => x.id);
                    table.ForeignKey(
                        name: "fk_warn_punishments_guild_config_guild_config_rel_id",
                        column: x => x.guild_id_fk,
                        principalTable: "guild_config",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stores punishments to be automatically applied once a user reaches a certain amount of warnings.");

            migrationBuilder.CreateTable(
                name: "auto_commands",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    timer_id_fk = table.Column<int>(type: "integer", nullable: true),
                    command_string = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    author_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auto_commands", x => x.id);
                    table.ForeignKey(
                        name: "fk_auto_commands_timers_timer_rel_id",
                        column: x => x.timer_id_fk,
                        principalTable: "timers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stores command data and the context it should be automatically sent to.");

            migrationBuilder.CreateTable(
                name: "reminders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    timer_id_fk = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    author_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    is_private = table.Column<bool>(type: "boolean", nullable: false),
                    elapse_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reminders", x => x.id);
                    table.ForeignKey(
                        name: "fk_reminders_timers_timer_rel_id",
                        column: x => x.timer_id_fk,
                        principalTable: "timers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stores reminder data and the context it should be sent to.");

            migrationBuilder.CreateTable(
                name: "repeaters",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    timer_id_fk = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    author_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    channel_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    interval = table.Column<TimeSpan>(type: "interval", nullable: false),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_repeaters", x => x.id);
                    table.ForeignKey(
                        name: "fk_repeaters_guild_config_guild_config_rel_id",
                        column: x => x.guild_id_fk,
                        principalTable: "guild_config",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_repeaters_timers_timer_rel_id",
                        column: x => x.timer_id_fk,
                        principalTable: "timers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stores repeater data and the context it should be sent to.");

            migrationBuilder.CreateTable(
                name: "warnings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    timer_id_fk = table.Column<int>(type: "integer", nullable: true),
                    guild_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    user_id_fk = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    author_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    warning_text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warnings", x => x.id);
                    table.ForeignKey(
                        name: "fk_warnings_discord_users_user_rel_id",
                        column: x => x.user_id_fk,
                        principalTable: "discord_users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_warnings_guild_config_guild_config_rel_id",
                        column: x => x.guild_id_fk,
                        principalTable: "guild_config",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_warnings_timers_timer_id_fk",
                        column: x => x.timer_id_fk,
                        principalTable: "timers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Stores warnings issued to users on servers.");

            migrationBuilder.CreateIndex(
                name: "ix_aliases_id",
                table: "aliases",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_auto_commands_timer_id_fk",
                table: "auto_commands",
                column: "timer_id_fk",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_command_cooldown_id",
                table: "command_cooldown",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_filtered_content_guild_id_fk",
                table: "filtered_content",
                column: "guild_id_fk");

            migrationBuilder.CreateIndex(
                name: "ix_filtered_words_guild_id_fk",
                table: "filtered_words",
                column: "guild_id_fk",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gatekeeping_guild_id_fk",
                table: "gatekeeping",
                column: "guild_id_fk",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_muted_users_guild_id_fk",
                table: "muted_users",
                column: "guild_id_fk");

            migrationBuilder.CreateIndex(
                name: "ix_occurrences_guild_id_fk",
                table: "occurrences",
                column: "guild_id_fk");

            migrationBuilder.CreateIndex(
                name: "ix_playing_statuses_id",
                table: "playing_statuses",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_polls_guild_id_fk",
                table: "polls",
                column: "guild_id_fk");

            migrationBuilder.CreateIndex(
                name: "ix_reminders_timer_id_fk",
                table: "reminders",
                column: "timer_id_fk",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_repeaters_guild_id_fk",
                table: "repeaters",
                column: "guild_id_fk");

            migrationBuilder.CreateIndex(
                name: "ix_repeaters_timer_id_fk",
                table: "repeaters",
                column: "timer_id_fk",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_timers_guild_id_fk",
                table: "timers",
                column: "guild_id_fk");

            migrationBuilder.CreateIndex(
                name: "ix_timers_id",
                table: "timers",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_timers_user_id_fk",
                table: "timers",
                column: "user_id_fk");

            migrationBuilder.CreateIndex(
                name: "ix_voice_roles_guild_id_fk",
                table: "voice_roles",
                column: "guild_id_fk");

            migrationBuilder.CreateIndex(
                name: "ix_warn_punishments_guild_id_fk",
                table: "warn_punishments",
                column: "guild_id_fk");

            migrationBuilder.CreateIndex(
                name: "ix_warnings_guild_id_fk",
                table: "warnings",
                column: "guild_id_fk");

            migrationBuilder.CreateIndex(
                name: "ix_warnings_timer_id_fk",
                table: "warnings",
                column: "timer_id_fk",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_warnings_user_id_fk",
                table: "warnings",
                column: "user_id_fk");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aliases");

            migrationBuilder.DropTable(
                name: "auto_commands");

            migrationBuilder.DropTable(
                name: "blacklist");

            migrationBuilder.DropTable(
                name: "command_cooldown");

            migrationBuilder.DropTable(
                name: "filtered_content");

            migrationBuilder.DropTable(
                name: "filtered_words");

            migrationBuilder.DropTable(
                name: "gatekeeping");

            migrationBuilder.DropTable(
                name: "muted_users");

            migrationBuilder.DropTable(
                name: "occurrences");

            migrationBuilder.DropTable(
                name: "playing_statuses");

            migrationBuilder.DropTable(
                name: "polls");

            migrationBuilder.DropTable(
                name: "reminders");

            migrationBuilder.DropTable(
                name: "repeaters");

            migrationBuilder.DropTable(
                name: "voice_roles");

            migrationBuilder.DropTable(
                name: "warn_punishments");

            migrationBuilder.DropTable(
                name: "warnings");

            migrationBuilder.DropTable(
                name: "timers");

            migrationBuilder.DropTable(
                name: "discord_users");

            migrationBuilder.DropTable(
                name: "guild_config");
        }
    }
}