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
        public DbSet<OccurrenceEntity> Occurrencies { get; set; }
        public DbSet<BlacklistEntity> Blacklist { get; set; }
        public DbSet<PlayingStatusEntity> PlayingStatuses { get; set; }

        public AkkoDbContext(DbContextOptions<AkkoDbContext> ctxOpt) : base(ctxOpt)
            => base.Database.Migrate(); // Ensure that the database exists

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
                .HasKey(x => x.Id); // Make this dependent on BotConfigEntity?

            #endregion

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

            #endregion

            modelBuilder.Entity<DiscordUserEntity>()
                .HasAlternateKey(x => x.UserId);

            modelBuilder.Entity<TimerEntity>()
                .HasIndex(x => x.Id);
        }
    }
}
