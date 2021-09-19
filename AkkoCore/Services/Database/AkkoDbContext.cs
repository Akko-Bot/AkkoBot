using AkkoCore.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace AkkoCore.Services.Database
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
        public DbSet<GuildLogEntity> GuildLogs { get; init; }
        public DbSet<TagEntity> Tags { get; init; }
        public DbSet<PermissionOverrideEntity> PermissionOverride { get; init; }

        public AkkoDbContext(DbContextOptions<AkkoDbContext> ctxOpt) : base(ctxOpt)
        {
        }

        // Reminder: PostgreSQL automatically indexes every unique constraint.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}