using AkkoBot.Services.Database.Repository;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Abstractions
{
    public interface IUnitOfWork : IDisposable
    {
        public DiscordUserRepo DiscordUsers { get; }
        public BlacklistRepo Blacklist { get; }
        public BotConfigRepo BotConfig { get; }
        public GuildConfigRepo GuildConfigs { get; }
        public PlayingStatusRepo PlayingStatuses { get; }

        int SaveChanges();
        Task<int> SaveChangesAsync();
    }
}
