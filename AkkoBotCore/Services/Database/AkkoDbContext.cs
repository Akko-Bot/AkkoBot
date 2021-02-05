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
        public DbSet<BlacklistEntity> Blacklist { get; set; }
        public DbSet<PlayingStatusEntity> PlayingStatuses { get; set; }

        public AkkoDbContext(DbContextOptions<AkkoDbContext> ctxOpt) : base(ctxOpt)
            => base.Database.Migrate(); // Ensure that the database exists

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DiscordUserEntity>()
                .HasIndex(u => u.UserId);

            modelBuilder.Entity<BotConfigEntity>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<GuildConfigEntity>()
                .HasIndex(g => g.GuildId);

            modelBuilder.Entity<TimerEntity>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<LogConfigEntity>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<BlacklistEntity>()
                .HasIndex(g => g.ContextId);

            modelBuilder.Entity<PlayingStatusEntity>()
                .HasNoKey(); // Make this dependent on BotConfigEntity?
        }
    }
}
