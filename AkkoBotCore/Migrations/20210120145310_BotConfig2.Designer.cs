﻿// <auto-generated />
using System;
using AkkoBot.Services.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace AkkoBot.Migrations
{
    [DbContext(typeof(AkkoDbContext))]
    [Migration("20210120145310_BotConfig2")]
    partial class BotConfig2
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseIdentityByDefaultColumns()
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.1");

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.BlacklistEntity", b =>
                {
                    b.Property<decimal>("ContextId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("context_id");

                    b.Property<DateTimeOffset>("DateAdded")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_added");

                    b.Property<string>("Name")
                        .HasMaxLength(37)
                        .HasColumnType("character varying(37)")
                        .HasColumnName("name");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasColumnName("type");

                    b.HasKey("ContextId")
                        .HasName("pk_blacklist");

                    b.HasIndex("ContextId")
                        .HasDatabaseName("ix_blacklist_context_id");

                    b.ToTable("blacklist");

                    b
                        .HasComment("Stores users, channels, and servers blacklisted from the bot.");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.BotConfigEntity", b =>
                {
                    b.Property<decimal>("BotId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("bot_id");

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

                    b.Property<bool>("EnableHelp")
                        .HasColumnType("boolean")
                        .HasColumnName("enable_help");

                    b.Property<string>("ErrorColor")
                        .IsRequired()
                        .HasMaxLength(6)
                        .HasColumnType("varchar(6)")
                        .HasColumnName("error_color");

                    b.Property<string>("Locale")
                        .IsRequired()
                        .HasMaxLength(6)
                        .HasColumnType("varchar(6)")
                        .HasColumnName("locale");

                    b.Property<string>("LogFormat")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("log_format");

                    b.Property<string>("LogTimeFormat")
                        .HasColumnType("varchar")
                        .HasColumnName("log_time_format");

                    b.Property<bool>("MentionPrefix")
                        .HasColumnType("boolean")
                        .HasColumnName("mention_prefix");

                    b.Property<int>("MessageSizeCache")
                        .HasColumnType("integer")
                        .HasColumnName("message_size_cache");

                    b.Property<string>("OkColor")
                        .IsRequired()
                        .HasMaxLength(6)
                        .HasColumnType("varchar(6)")
                        .HasColumnName("ok_color");

                    b.Property<bool>("RespondToDms")
                        .HasColumnType("boolean")
                        .HasColumnName("respond_to_dms");

                    b.Property<bool>("UseEmbed")
                        .HasColumnType("boolean")
                        .HasColumnName("use_embed");

                    b.HasKey("BotId")
                        .HasName("pk_bot_config");

                    b.HasIndex("BotId")
                        .HasDatabaseName("ix_bot_config_bot_id");

                    b.ToTable("bot_config");

                    b
                        .HasComment("Stores settings related to the bot.");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.DiscordUserEntity", b =>
                {
                    b.Property<decimal>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("user_id");

                    b.Property<DateTimeOffset>("DateAdded")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_added");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasMaxLength(4)
                        .HasColumnType("varchar(4)")
                        .HasColumnName("discriminator");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)")
                        .HasColumnName("username");

                    b.HasKey("UserId")
                        .HasName("pk_discord_users");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_discord_users_user_id");

                    b.ToTable("discord_users");

                    b
                        .HasComment("Stores data and settings related to individual Discord users.");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.GuildConfigEntity", b =>
                {
                    b.Property<decimal>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("guild_id");

                    b.Property<DateTimeOffset>("DateAdded")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_added");

                    b.Property<string>("ErrorColor")
                        .IsRequired()
                        .HasMaxLength(6)
                        .HasColumnType("varchar(6)")
                        .HasColumnName("error_color");

                    b.Property<string>("Locale")
                        .IsRequired()
                        .HasMaxLength(6)
                        .HasColumnType("varchar(6)")
                        .HasColumnName("locale");

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

                    b.HasKey("GuildId")
                        .HasName("pk_guild_configs");

                    b.HasIndex("GuildId")
                        .HasDatabaseName("ix_guild_configs_guild_id");

                    b.ToTable("guild_configs");

                    b
                        .HasComment("Stores settings related to individual Discord servers.");
                });

            modelBuilder.Entity("AkkoBot.Services.Database.Entities.PlayingStatusEntity", b =>
                {
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

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasColumnName("type");

                    b.ToTable("playing_statuses");

                    b
                        .HasComment("Stores data related to the bot's Discord status.");
                });
#pragma warning restore 612, 618
        }
    }
}