﻿using AkkoBot.Services.Database.Entities;
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

        public AkkoDbContext(DbContextOptions<AkkoDbContext> ctxOpt) : base(ctxOpt) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DiscordUserEntity>()
                .HasIndex(u => u.UserId);

            modelBuilder.Entity<BotConfigEntity>()
                .HasNoKey();

            modelBuilder.Entity<GuildConfigEntity>()
                .HasIndex(g => g.GuildId);
                //.IncludeProperties(p => p.Prefix)
                //.IncludeProperties(p => p.UseEmbed)
                //.IncludeProperties(p => p.OkColor)
                //.IncludeProperties(p => p.ErrorColor);

            modelBuilder.Entity<BlacklistEntity>()
                .HasIndex(g => g.TypeId);

            modelBuilder.Entity<PlayingStatusEntity>()
                .HasNoKey();
        }
    }
}
