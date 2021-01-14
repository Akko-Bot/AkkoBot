using AkkoBot.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;


namespace AkkoBot.Services.Database
{
    public class AkkoDbContext : DbContext
    {
        public DbSet<DiscordUserEntity> DiscordUsers { get; set; }
        public DbSet<BotConfigEntity> BotConfig { get; set; }
        public DbSet<GuildConfigEntity> GuildConfigs { get; set; }
        public DbSet<BlacklistEntity> Blacklist { get; set; }
        public DbSet<PlayingStatusEntity> PlayingStatuses { get; set; }

        public AkkoDbContext(DbContextOptions<AkkoDbContext> ctxOpt) : base(ctxOpt)
        {
            // Ensure that the database exists
            base.Database.Migrate();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DiscordUserEntity>()
                .HasIndex(u => u.UserId);

            modelBuilder.Entity<BotConfigEntity>()
                .HasNoKey();

            modelBuilder.Entity<GuildConfigEntity>()
                .HasIndex(g => g.GuildId);

            modelBuilder.Entity<BlacklistEntity>()
                .HasIndex(g => g.ContextId);

            modelBuilder.Entity<PlayingStatusEntity>()
                .HasNoKey();
        }
    }
}
