using AkkoBot.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database
{
    public class AkkoDbContext : DbContext
    {
        public DbSet<DiscordUserEntity> DiscordUsers { get; init; }
        public DbSet<BotConfigEntity> BotConfig { get; init; }
        public DbSet<GuildConfigEntity> GuildConfig { get; init; }
        public DbSet<LogConfigEntity> LogConfig { get; init; }
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
        public DbSet<CommandEntity> AutoCommands { get; init; }
        public DbSet<VoiceRoleEntity> VoiceRoles { get; init; }
        public DbSet<CommandCooldownEntity> CommandCooldown { get; init; }
        public DbSet<PollEntity> Polls { get; init; }

        public AkkoDbContext(DbContextOptions<AkkoDbContext> ctxOpt) : base(ctxOpt)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region Bot Configuration

            modelBuilder.Entity<BotConfigEntity>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<LogConfigEntity>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<BlacklistEntity>()
                .HasAlternateKey(g => g.ContextId);

            modelBuilder.Entity<PlayingStatusEntity>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<AliasEntity>()
                .HasKey(x => x.Id);

            #endregion Bot Configuration

            #region Guild Configuration

            modelBuilder.Entity<GuildConfigEntity>()
                .HasAlternateKey(x => x.GuildId);

            modelBuilder.Entity<MutedUserEntity>()
                .HasOne(x => x.GuildConfigRel)
                .WithMany(x => x.MutedUserRel)
                .HasForeignKey(x => x.GuildIdFK)
                .HasPrincipalKey(x => x.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WarnEntity>()
                .HasOne(x => x.GuildConfigRel)
                .WithMany(x => x.WarnRel)
                .HasForeignKey(x => x.GuildIdFK)
                .HasPrincipalKey(x => x.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WarnPunishEntity>()
                .HasOne(x => x.GuildConfigRel)
                .WithMany(x => x.WarnPunishRel)
                .HasForeignKey(x => x.GuildIdFK)
                .HasPrincipalKey(x => x.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OccurrenceEntity>()
                .HasOne(x => x.GuildConfigRel)
                .WithMany(x => x.OccurrenceRel)
                .HasForeignKey(x => x.GuildIdFK)
                .HasPrincipalKey(x => x.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FilteredWordsEntity>()
                .HasOne(x => x.GuildConfigRel)
                .WithOne(x => x.FilteredWordsRel)
                .HasForeignKey<FilteredWordsEntity>(x => x.GuildIdFK)
                .HasPrincipalKey<GuildConfigEntity>(x => x.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FilteredContentEntity>()
                .HasOne(x => x.GuildConfigRel)
                .WithMany(x => x.FilteredContentRel)
                .HasForeignKey(x => x.GuildIdFK)
                .HasPrincipalKey(x => x.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VoiceRoleEntity>()
                .HasOne(x => x.GuildConfigRel)
                .WithMany(x => x.VoiceRolesRel)
                .HasForeignKey(x => x.GuildIdFk)
                .HasPrincipalKey(x => x.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PollEntity>()
                .HasOne(x => x.GuildConfigRel)
                .WithMany(x => x.PollRel)
                .HasForeignKey(x => x.GuildIdFK)
                .HasPrincipalKey(x => x.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            #endregion Guild Configuration

            modelBuilder.Entity<DiscordUserEntity>()
                .HasAlternateKey(x => x.UserId);

            modelBuilder.Entity<TimerEntity>()
                .HasIndex(x => x.Id);

            modelBuilder.Entity<ReminderEntity>()
                .HasIndex(x => x.Id);

            modelBuilder.Entity<CommandEntity>()
                .HasIndex(x => x.Id);

            modelBuilder.Entity<CommandCooldownEntity>()
                .HasIndex(x => x.Id);
        }
    }
}