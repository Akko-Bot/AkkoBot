using AkkoBot.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database
{
    /// <summary>
    /// Represents a session with Akko's core database and can be used to insert and retrieve data from it.
    /// </summary>
    public sealed class AkkoDbContext : DbContext
    {
        public DbSet<DiscordUserEntity> DiscordUsers { get; init; }
        public DbSet<GuildConfigEntity> GuildConfig { get; init; }
        public DbSet<TimerEntity> Timers { get; init; }
        public DbSet<MutedUserEntity> MutedUsers { get; init; }
        public DbSet<WarnEntity> Warnings { get; init; }
        public DbSet<WarnPunishEntity> WarnPunishments { get; init; }
        public DbSet<OccurrenceEntity> Occurrences { get; init; }
        public DbSet<BlacklistEntity> Blacklist { get; init; }
        public DbSet<PlayingStatusEntity> PlayingStatuses { get; init; }
        public DbSet<AliasEntity> Aliases { get; init; }
        public DbSet<FilteredWordsEntity> FilteredWords { get; init; }
        public DbSet<FilteredContentEntity> FilteredContent { get; init; }
        public DbSet<ReminderEntity> Reminders { get; init; }
        public DbSet<RepeaterEntity> Repeaters { get; init; }
        public DbSet<AutoCommandEntity> AutoCommands { get; init; }
        public DbSet<VoiceRoleEntity> VoiceRoles { get; init; }
        public DbSet<CommandCooldownEntity> CommandCooldown { get; init; }
        public DbSet<PollEntity> Polls { get; init; }
        public DbSet<GatekeepEntity> Gatekeeping { get; init; }
        public DbSet<AutoSlowmodeEntity> AutoSlowmode { get; init; }

        public AkkoDbContext(DbContextOptions<AkkoDbContext> ctxOpt) : base(ctxOpt)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region Global Configuration

            modelBuilder.Entity<BlacklistEntity>()
                .HasAlternateKey(g => g.ContextId);

            modelBuilder.Entity<PlayingStatusEntity>()
                .HasIndex(x => x.Id);

            modelBuilder.Entity<AliasEntity>()
                .HasIndex(x => x.Id);

            modelBuilder.Entity<CommandCooldownEntity>()
                .HasIndex(x => x.Id);

            #endregion Global Configuration

            #region Guild Configuration

            modelBuilder.Entity<GuildConfigEntity>()
                .HasAlternateKey(x => x.GuildId);

            // Guild -> Muted User
            modelBuilder.Entity<MutedUserEntity>()
                .HasOne(x => x.GuildConfigRel)
                .WithMany(x => x.MutedUserRel)
                .HasForeignKey(x => x.GuildIdFK)
                .HasPrincipalKey(x => x.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            // Guild -> Infractions
            modelBuilder.Entity<WarnEntity>()
                .HasOne(x => x.GuildConfigRel)
                .WithMany(x => x.WarnRel)
                .HasForeignKey(x => x.GuildIdFK)
                .HasPrincipalKey(x => x.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            // Guild -> Punishments
            modelBuilder.Entity<WarnPunishEntity>()
                .HasOne(x => x.GuildConfigRel)
                .WithMany(x => x.WarnPunishRel)
                .HasForeignKey(x => x.GuildIdFK)
                .HasPrincipalKey(x => x.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            // Guild -> Occurrences
            modelBuilder.Entity<OccurrenceEntity>()
                .HasOne(x => x.GuildConfigRel)
                .WithMany(x => x.OccurrenceRel)
                .HasForeignKey(x => x.GuildIdFK)
                .HasPrincipalKey(x => x.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            // Guild -> Filtered Words
            modelBuilder.Entity<FilteredWordsEntity>()
                .HasOne(x => x.GuildConfigRel)
                .WithOne(x => x.FilteredWordsRel)
                .HasForeignKey<FilteredWordsEntity>(x => x.GuildIdFK)
                .HasPrincipalKey<GuildConfigEntity>(x => x.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            // Guild -> Filtered Content
            modelBuilder.Entity<FilteredContentEntity>()
                .HasOne(x => x.GuildConfigRel)
                .WithMany(x => x.FilteredContentRel)
                .HasForeignKey(x => x.GuildIdFK)
                .HasPrincipalKey(x => x.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            // Guild -> Voice Roles
            modelBuilder.Entity<VoiceRoleEntity>()
                .HasOne(x => x.GuildConfigRel)
                .WithMany(x => x.VoiceRolesRel)
                .HasForeignKey(x => x.GuildIdFk)
                .HasPrincipalKey(x => x.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            // Guild -> Polls
            modelBuilder.Entity<PollEntity>()
                .HasOne(x => x.GuildConfigRel)
                .WithMany(x => x.PollRel)
                .HasForeignKey(x => x.GuildIdFK)
                .HasPrincipalKey(x => x.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            // Guild -> Repeaters
            modelBuilder.Entity<RepeaterEntity>()
                .HasOne(x => x.GuildConfigRel)
                .WithMany(x => x.RepeaterRel)
                .HasForeignKey(x => x.GuildIdFK)
                .HasPrincipalKey(x => x.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            // Guild -> Timers
            modelBuilder.Entity<TimerEntity>()
                .HasOne(x => x.GuildConfigRel)
                .WithMany(x => x.TimerRel)
                .HasForeignKey(x => x.GuildIdFK)
                .HasPrincipalKey(x => x.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            // Guild -> Gatekeeping
            modelBuilder.Entity<GatekeepEntity>()
                .HasOne(x => x.GuildConfigRel)
                .WithOne(x => x.GatekeepRel)
                .HasForeignKey<GatekeepEntity>(x => x.GuildIdFK)
                .HasPrincipalKey<GuildConfigEntity>(x => x.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            // Guild -> Autoslowmode
            modelBuilder.Entity<AutoSlowmodeEntity>()
                .HasOne(x => x.GuildConfigRel)
                .WithOne(x => x.AutoSlowmodeRel)
                .HasForeignKey<AutoSlowmodeEntity>(x => x.GuildIdFK)
                .HasPrincipalKey<GuildConfigEntity>(x => x.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            #endregion Guild Configuration

            #region Timer Entities

            modelBuilder.Entity<TimerEntity>()
                .HasIndex(x => x.Id);

            // Timer -> Repeater
            modelBuilder.Entity<RepeaterEntity>()
                .HasOne(x => x.TimerRel)
                .WithOne()
                .HasForeignKey<RepeaterEntity>(x => x.TimerIdFK)
                .HasPrincipalKey<TimerEntity>(x => x.Id)
                .OnDelete(DeleteBehavior.Cascade);

            // Timer -> Reminder
            modelBuilder.Entity<ReminderEntity>()
                .HasOne(x => x.TimerRel)
                .WithOne()
                .HasForeignKey<ReminderEntity>(x => x.TimerIdFK)
                .HasPrincipalKey<TimerEntity>(x => x.Id)
                .OnDelete(DeleteBehavior.Cascade);

            // Timer -> Autocommand
            modelBuilder.Entity<AutoCommandEntity>()
                .HasOne(x => x.TimerRel)
                .WithOne()
                .HasForeignKey<AutoCommandEntity>(x => x.TimerIdFK)
                .HasPrincipalKey<TimerEntity>(x => x.Id)
                .OnDelete(DeleteBehavior.Cascade);

            // Timer -> Infraction
            modelBuilder.Entity<WarnEntity>()
                .HasOne(x => x.TimerRel)
                .WithOne(x => x.WarnRel)
                .HasForeignKey<WarnEntity>(x => x.TimerIdFK)
                .HasPrincipalKey<TimerEntity>(x => x.Id)
                .OnDelete(DeleteBehavior.Restrict);

            #endregion Timer Entities

            #region Miscellaneous

            modelBuilder.Entity<DiscordUserEntity>()
                .HasAlternateKey(x => x.UserId);

            // User -> Timers
            modelBuilder.Entity<TimerEntity>()
                .HasOne(x => x.UserRel)
                .WithMany(x => x.TimerRel)
                .HasForeignKey(x => x.UserIdFK)
                .HasPrincipalKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> Warnings
            modelBuilder.Entity<WarnEntity>()
                .HasOne(x => x.UserRel)
                .WithMany(x => x.WarnRel)
                .HasForeignKey(x => x.UserIdFK)
                .HasPrincipalKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            #endregion Miscellaneous
        }
    }
}