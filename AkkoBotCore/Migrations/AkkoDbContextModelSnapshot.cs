﻿// <auto-generated />
using System;
using System.Collections.Generic;
using AkkoBot.Services.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace AkkoBot.Migrations
{
    [DbContext(typeof(AkkoDbContext))]
    partial class AkkoDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseIdentityByDefaultColumns()
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.1");

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.AliasEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<string>("Alias")
                        .IsRequired()
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)")
                        .HasColumnName("alias");

                    b.Property<string>("Arguments")
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)")
                        .HasColumnName("arguments");

                    b.Property<string>("Command")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("command");

                    b.Property<DateTimeOffset>("DateAdded")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_added");

                    b.Property<decimal?>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<bool>("IsDynamic")
                        .HasColumnType("boolean")
                        .HasColumnName("is_dynamic");

                    b.HasKey("Id")
                        .HasName("pk_aliases");

                    b.ToTable("aliases");

                    b
                        .HasComment("Stores command aliases.");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.BlacklistEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<decimal>("ContextId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("context_id");

                    b.Property<DateTimeOffset>("DateAdded")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_added");

                    b.Property<string>("Name")
                        .HasMaxLength(37)
                        .HasColumnType("character varying(37)")
                        .HasColumnName("name");

                    b.Property<string>("Reason")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("reason");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasColumnName("type");

                    b.HasKey("Id")
                        .HasName("pk_blacklist");

                    b.HasAlternateKey("ContextId")
                        .HasName("ak_blacklist_context_id");

                    b.ToTable("blacklist");

                    b
                        .HasComment("Stores users, channels, and servers blacklisted from the bot.");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.BotConfigEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<string>("BotPrefix")
                        .IsRequired()
                        .HasMaxLength(15)
                        .HasColumnType("character varying(15)")
                        .HasColumnName("bot_prefix");

                    b.Property<bool>("CaseSensitiveCommands")
                        .HasColumnType("boolean")
                        .HasColumnName("case_sensitive_commands");

                    b.Property<DateTimeOffset>("DateAdded")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_added");

                    b.Property<List<string>>("DisabledCommands")
                        .HasColumnType("text[]")
                        .HasColumnName("disabled_commands");

                    b.Property<bool>("EnableHelp")
                        .HasColumnType("boolean")
                        .HasColumnName("enable_help");

                    b.Property<string>("ErrorColor")
                        .IsRequired()
                        .HasMaxLength(6)
                        .HasColumnType("varchar(6)")
                        .HasColumnName("error_color");

                    b.Property<TimeSpan?>("InteractiveTimeout")
                        .IsRequired()
                        .HasColumnType("interval")
                        .HasColumnName("interactive_timeout");

                    b.Property<string>("Locale")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("varchar(10)")
                        .HasColumnName("locale");

                    b.Property<bool>("MentionPrefix")
                        .HasColumnType("boolean")
                        .HasColumnName("mention_prefix");

                    b.Property<int>("MessageSizeCache")
                        .HasColumnType("integer")
                        .HasColumnName("message_size_cache");

                    b.Property<TimeSpan>("MinWarnExpire")
                        .HasColumnType("interval")
                        .HasColumnName("min_warn_expire");

                    b.Property<string>("OkColor")
                        .IsRequired()
                        .HasMaxLength(6)
                        .HasColumnType("varchar(6)")
                        .HasColumnName("ok_color");

                    b.Property<bool>("RespondToDms")
                        .HasColumnType("boolean")
                        .HasColumnName("respond_to_dms");

                    b.Property<bool>("RotateStatus")
                        .HasColumnType("boolean")
                        .HasColumnName("rotate_status");

                    b.Property<bool>("UseEmbed")
                        .HasColumnType("boolean")
                        .HasColumnName("use_embed");

                    b.HasKey("Id")
                        .HasName("pk_bot_config");

                    b.ToTable("bot_config");

                    b
                        .HasComment("Stores settings related to the bot.");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.DiscordUserEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<DateTimeOffset>("DateAdded")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_added");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasMaxLength(4)
                        .HasColumnType("varchar(4)")
                        .HasColumnName("discriminator");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)")
                        .HasColumnName("username");

                    b.HasKey("Id")
                        .HasName("pk_discord_users");

                    b.HasAlternateKey("UserId")
                        .HasName("ak_discord_users_user_id");

                    b.ToTable("discord_users");

                    b
                        .HasComment("Stores data and settings related to individual Discord users.");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.FilteredWordsEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<DateTimeOffset>("DateAdded")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_added");

                    b.Property<bool>("Enabled")
                        .HasColumnType("boolean")
                        .HasColumnName("enabled");

                    b.Property<decimal>("GuildIdFK")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id_fk");

                    b.Property<List<long>>("IgnoredIds")
                        .HasColumnType("bigint[]")
                        .HasColumnName("ignored_ids");

                    b.Property<string>("NotificationMessage")
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)")
                        .HasColumnName("notification_message");

                    b.Property<bool>("NotifyOnDelete")
                        .HasColumnType("boolean")
                        .HasColumnName("notify_on_delete");

                    b.Property<bool>("WarnOnDelete")
                        .HasColumnType("boolean")
                        .HasColumnName("warn_on_delete");

                    b.Property<List<string>>("Words")
                        .HasColumnType("text[]")
                        .HasColumnName("words");

                    b.HasKey("Id")
                        .HasName("pk_filtered_words");

                    b.HasIndex("GuildIdFK")
                        .IsUnique()
                        .HasDatabaseName("ix_filtered_words_guild_id_fk");

                    b.ToTable("filtered_words");

                    b
                        .HasComment("Stores filtered words of a Discord server.");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.GuildConfigEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<DateTimeOffset>("DateAdded")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_added");

                    b.Property<string>("ErrorColor")
                        .IsRequired()
                        .HasMaxLength(6)
                        .HasColumnType("varchar(6)")
                        .HasColumnName("error_color");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<TimeSpan?>("InteractiveTimeout")
                        .HasColumnType("interval")
                        .HasColumnName("interactive_timeout");

                    b.Property<string>("Locale")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("varchar(10)")
                        .HasColumnName("locale");

                    b.Property<decimal?>("MuteRoleId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("mute_role_id");

                    b.Property<string>("OkColor")
                        .IsRequired()
                        .HasMaxLength(6)
                        .HasColumnType("varchar(6)")
                        .HasColumnName("ok_color");

                    b.Property<string>("Prefix")
                        .IsRequired()
                        .HasMaxLength(15)
                        .HasColumnType("character varying(15)")
                        .HasColumnName("prefix");

                    b.Property<bool>("UseEmbed")
                        .HasColumnType("boolean")
                        .HasColumnName("use_embed");

                    b.Property<TimeSpan>("WarnExpire")
                        .HasColumnType("interval")
                        .HasColumnName("warn_expire");

                    b.HasKey("Id")
                        .HasName("pk_guild_config");

                    b.HasAlternateKey("GuildId")
                        .HasName("ak_guild_config_guild_id");

                    b.ToTable("guild_config");

                    b
                        .HasComment("Stores settings related to individual Discord servers.");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.LogConfigEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<DateTimeOffset>("DateAdded")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_added");

                    b.Property<bool>("IsLoggedToFile")
                        .HasColumnType("boolean")
                        .HasColumnName("is_logged_to_file");

                    b.Property<string>("LogFormat")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("log_format");

                    b.Property<int>("LogLevel")
                        .HasColumnType("integer")
                        .HasColumnName("log_level");

                    b.Property<double>("LogSizeMB")
                        .HasColumnType("double precision")
                        .HasColumnName("log_size_mb");

                    b.Property<string>("LogTimeFormat")
                        .HasColumnType("varchar")
                        .HasColumnName("log_time_format");

                    b.HasKey("Id")
                        .HasName("pk_log_config");

                    b.ToTable("log_config");

                    b
                        .HasComment("Stores data and settings related to how the bot logs command usage.");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.MutedUserEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<DateTimeOffset>("DateAdded")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_added");

                    b.Property<decimal>("GuildIdFK")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id_fk");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_muted_users");

                    b.HasIndex("GuildIdFK")
                        .HasDatabaseName("ix_muted_users_guild_id_fk");

                    b.ToTable("muted_users");

                    b
                        .HasComment("Stores data about users that got muted in a specific server.");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.OccurrenceEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<int>("Bans")
                        .HasColumnType("integer")
                        .HasColumnName("bans");

                    b.Property<DateTimeOffset>("DateAdded")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_added");

                    b.Property<decimal>("GuildIdFK")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id_fk");

                    b.Property<int>("Kicks")
                        .HasColumnType("integer")
                        .HasColumnName("kicks");

                    b.Property<int>("Mutes")
                        .HasColumnType("integer")
                        .HasColumnName("mutes");

                    b.Property<int>("Notices")
                        .HasColumnType("integer")
                        .HasColumnName("notices");

                    b.Property<int>("Softbans")
                        .HasColumnType("integer")
                        .HasColumnName("softbans");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.Property<int>("Warnings")
                        .HasColumnType("integer")
                        .HasColumnName("warnings");

                    b.HasKey("Id")
                        .HasName("pk_occurrencies");

                    b.HasIndex("GuildIdFK")
                        .HasDatabaseName("ix_occurrencies_guild_id_fk");

                    b.ToTable("occurrencies");

                    b
                        .HasComment("Stores how many times a user got punished in a server.");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.PlayingStatusEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<DateTimeOffset>("DateAdded")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_added");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)")
                        .HasColumnName("message");

                    b.Property<TimeSpan>("RotationTime")
                        .HasColumnType("interval")
                        .HasColumnName("rotation_time");

                    b.Property<string>("StreamUrl")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("stream_url");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasColumnName("type");

                    b.HasKey("Id")
                        .HasName("pk_playing_statuses");

                    b.ToTable("playing_statuses");

                    b
                        .HasComment("Stores data related to the bot's Discord status.");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.TimerEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<decimal?>("ChannelId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("channel_id");

                    b.Property<DateTimeOffset>("DateAdded")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_added");

                    b.Property<DateTimeOffset>("ElapseAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("elapse_at");

                    b.Property<decimal?>("GuildId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<TimeSpan>("Interval")
                        .HasColumnType("interval")
                        .HasColumnName("interval");

                    b.Property<bool>("IsAbsolute")
                        .HasColumnType("boolean")
                        .HasColumnName("is_absolute");

                    b.Property<bool>("IsRepeatable")
                        .HasColumnType("boolean")
                        .HasColumnName("is_repeatable");

                    b.Property<decimal?>("RoleId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("role_id");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasColumnName("type");

                    b.Property<decimal?>("UserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_timers");

                    b.HasIndex("Id")
                        .HasDatabaseName("ix_timers_id");

                    b.ToTable("timers");

                    b
                        .HasComment("Stores actions that need to be performed at some point in the future.");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.WarnEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<decimal>("AuthorId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("author_id");

                    b.Property<DateTimeOffset>("DateAdded")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_added");

                    b.Property<decimal>("GuildIdFK")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id_fk");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasColumnName("type");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.Property<string>("WarningText")
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)")
                        .HasColumnName("warning_text");

                    b.HasKey("Id")
                        .HasName("pk_warnings");

                    b.HasIndex("GuildIdFK")
                        .HasDatabaseName("ix_warnings_guild_id_fk");

                    b.ToTable("warnings");

                    b
                        .HasComment("Stores warnings issued to users on servers.");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.WarnPunishEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .UseIdentityByDefaultColumn();

                    b.Property<DateTimeOffset>("DateAdded")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_added");

                    b.Property<decimal>("GuildIdFK")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id_fk");

                    b.Property<TimeSpan?>("Interval")
                        .HasColumnType("interval")
                        .HasColumnName("interval");

                    b.Property<decimal?>("PunishRoleId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("punish_role_id");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasColumnName("type");

                    b.Property<int>("WarnAmount")
                        .HasColumnType("integer")
                        .HasColumnName("warn_amount");

                    b.HasKey("Id")
                        .HasName("pk_warn_punishments");

                    b.HasIndex("GuildIdFK")
                        .HasDatabaseName("ix_warn_punishments_guild_id_fk");

                    b.ToTable("warn_punishments");

                    b
                        .HasComment("Stores punishments to be automatically applied once a user reaches a certain amount of warnings.");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.FilteredWordsEntity", b =>
                {
                    b.HasOne("AkkoBot.Services.Database.Entities.GuildConfigEntity", "GuildConfigRel")
                        .WithOne("FilteredWordsRel")
                        .HasForeignKey("AkkoBot.Services.Database.Entities.FilteredWordsEntity", "GuildIdFK")
                        .HasConstraintName("fk_filtered_words_guild_config_guild_config_rel_id")
                        .HasPrincipalKey("AkkoBot.Services.Database.Entities.GuildConfigEntity", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("GuildConfigRel");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.MutedUserEntity", b =>
                {
                    b.HasOne("AkkoBot.Services.Database.Entities.GuildConfigEntity", "GuildConfigRel")
                        .WithMany("MutedUserRel")
                        .HasForeignKey("GuildIdFK")
                        .HasConstraintName("fk_muted_users_guild_config_guild_config_rel_id")
                        .HasPrincipalKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("GuildConfigRel");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.OccurrenceEntity", b =>
                {
                    b.HasOne("AkkoBot.Services.Database.Entities.GuildConfigEntity", "GuildConfigRel")
                        .WithMany("OccurrenceRel")
                        .HasForeignKey("GuildIdFK")
                        .HasConstraintName("fk_occurrencies_guild_config_guild_config_rel_id")
                        .HasPrincipalKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("GuildConfigRel");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.WarnEntity", b =>
                {
                    b.HasOne("AkkoBot.Services.Database.Entities.GuildConfigEntity", "GuildConfigRel")
                        .WithMany("WarnRel")
                        .HasForeignKey("GuildIdFK")
                        .HasConstraintName("fk_warnings_guild_config_guild_config_rel_id")
                        .HasPrincipalKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("GuildConfigRel");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.WarnPunishEntity", b =>
                {
                    b.HasOne("AkkoBot.Services.Database.Entities.GuildConfigEntity", "GuildConfigRel")
                        .WithMany("WarnPunishRel")
                        .HasForeignKey("GuildIdFK")
                        .HasConstraintName("fk_warn_punishments_guild_config_guild_config_rel_id")
                        .HasPrincipalKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("GuildConfigRel");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.GuildConfigEntity", b =>
                {
                    b.Navigation("FilteredWordsRel");

                    b.Navigation("MutedUserRel");

                    b.Navigation("OccurrenceRel");

                    b.Navigation("WarnPunishRel");

                    b.Navigation("WarnRel");
                });
#pragma warning restore 612, 618
        }
    }
}
