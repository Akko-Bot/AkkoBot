using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Database.Repository;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Abstractions
{
    public interface IUnitOfWork
    {
        DiscordUserRepo DiscordUsers { get; }
        DbRepository<BlacklistEntity> Blacklist { get; }
        DbRepository<BotConfigEntity> BotConfig { get; }
        GuildConfigRepo GuildConfigs { get; }
        DbRepository<PlayingStatusEntity> PlayingStatuses { get; }

        int SaveChanges();
        Task<int> SaveChangesAsync();
    }
}
