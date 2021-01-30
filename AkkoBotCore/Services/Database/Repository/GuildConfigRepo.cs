using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus.Entities;
using System.Collections.Concurrent;

namespace AkkoBot.Services.Database.Repository
{
    public class GuildConfigRepo : DbRepository<GuildConfigEntity>
    {
        private readonly BotConfigEntity _botConfig;
        public ConcurrentDictionary<ulong, GuildConfigEntity> Cache { get; }

        public GuildConfigRepo(AkkoDbContext db, IDbCacher dbCacher) : base(db)
        {
            _botConfig = dbCacher.BotConfig;
            Cache = dbCacher.Guilds;
        }

        /// <summary>
        /// Gets the settings of the specified Discord guild.
        /// </summary>
        /// <param name="sid">The ID of the Discord guild.</param>
        /// <returns>The guild settings.</returns>
        public GuildConfigEntity GetGuild(ulong sid)
        {
            if (Cache.ContainsKey(sid))
                return Cache[sid];
            else
            {
                var guild = base.GetSync(sid);
                Cache.TryAdd(guild.GuildId, guild);

                return guild;
            }
        }

        /// <summary>
        /// Adds an entry for the specified guild into the database.
        /// </summary>
        /// <param name="guild">The ID of the Discord guild.</param>
        /// <remarks>If an entry for the guild already exists, it does nothing.</remarks>
        /// <returns><see langword="true"/> if the entry got added to EF Core's tracker or to the cache, <see langword="false"/> otherwise.</returns>
        public bool TryCreate(DiscordGuild guild)
        {
            if (!Cache.ContainsKey(guild.Id))
            {
                var dGuild = new GuildConfigEntity(_botConfig) { GuildId = guild.Id };
                                
                base.Create(dGuild);                    // Add to the database
                Cache.TryAdd(dGuild.GuildId, dGuild);   // Add to the cache

                return true;
            }

            return false;
        }

        /// <summary>
        /// Upserts an entry for the specified guild into the database.
        /// </summary>
        /// <param name="guild">The ID of the Discord guild.</param>
        /// <remarks>This method will always add an entry to EF Core's tracker.</remarks>
        /// <returns><see langword="true"/> if the entry got added to the cache, <see langword="false"/> if it got updated.</returns>
        public bool CreateOrUpdate(GuildConfigEntity guild)
        {
            if (Cache.ContainsKey(guild.GuildId))
            {
                base.Update(guild);
                Cache.TryUpdate(guild.GuildId, guild, Cache[guild.GuildId]);
                return false;
            }
            else
            {
                base.Create(guild);
                Cache.TryAdd(guild.GuildId, guild);
                return true;
            }
        }
    }
}
