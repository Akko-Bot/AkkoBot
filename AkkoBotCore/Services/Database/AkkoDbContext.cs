using AkkoBot.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database
{
    public class AkkoDbContext : DbContext
    {
        public DbSet<DiscordUserEntity> DiscordUsers { get; set; }
        public DbSet<BotConfigEntity> BotConfig { get; set; }
        public DbSet<GuildConfigEntity> GuildConfig { get; set; }
        public DbSet<LogConfigEntity> LogConfig { get; set; }
        public DbSet<TimerEntity> Timers { get; set; }
        public DbSet<MutedUserEntity> MutedUsers { get; set; }
        public DbSet<WarnEntity> Warnings { get; set; }
        public DbSet<WarnPunishEntity> WarnPunishments { get; set; }
        public DbSet<OccurrenceEntity> Occurrences { get; set; }
        public DbSet<BlacklistEntity> Blacklist { get; set; }
        public DbSet<PlayingStatusEntity> PlayingStatuses { get; set; }
        public DbSet<AliasEntity> Aliases { get; set; }
        public DbSet<FilteredWordsEntity> FilteredWords { get; set; }
        public DbSet<ReminderEntity> Reminders { get; set; }
        public DbSet<CommandEntity> AutoCommands { get; set; }

        public AkkoDbContext(DbContextOptions<AkkoDbContext> ctxOpt) : base(ctxOpt) { }

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

            #endregion Guild Configuration

            modelBuilder.Entity<DiscordUserEntity>()
                .HasAlternateKey(x => x.UserId);

            modelBuilder.Entity<TimerEntity>()
                .HasIndex(x => x.Id);

            modelBuilder.Entity<ReminderEntity>()
                .HasIndex(x => x.Id);

            modelBuilder.Entity<CommandEntity>()
                .HasIndex(x => x.Id);
        }
    }
}