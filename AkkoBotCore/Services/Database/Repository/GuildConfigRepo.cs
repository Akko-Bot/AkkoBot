using System.Linq;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus.Entities;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

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
        /// <returns>
        /// The guild settings, <see langword="null"/> if for some reason the bot tries
        /// to get a guild it has never been to.
        /// </returns>
        public GuildConfigEntity GetGuild(ulong sid)
        {
            if (Cache.ContainsKey(sid))
                return Cache[sid];
            else
            {
                var guild = base.Table.FirstOrDefault(x => x.GuildId == sid);

                if (guild is not null)
                    Cache.TryAdd(guild.GuildId, guild);

                return guild;
            }
        }

        /// <summary>
        /// Gets the settings of the specified Discord guild with the warnings of a specific user.
        /// </summary>
        /// <param name="sid">The ID of the Discord guild.</param>
        /// <param name="uid">The ID of the Discord user.</param>
        /// <param name="type">The type of entry to be collected</param>
        /// <remarks>The warnings will be empty if the user has none. This overload returns entries specified by <paramref name="type"/>.</remarks>
        /// <returns>The guild settings, <see langword="null"/> if for some reason the guild doesn't exist in the database.</returns>
        public async Task<GuildConfigEntity> GetGuildWithWarningsAsync(ulong sid, ulong uid, WarnType type)
        {
            return await base.Table
                .Include(x => x.WarnRel.Where(x => x.UserId == uid && x.Type == type))
                .Include(x => x.WarnPunishRel)
                .FirstOrDefaultAsync(x => x.GuildId == sid);
        }

        /// <summary>
        /// Gets the settings of the specified Discord guild with the warnings of a specific user.
        /// </summary>
        /// <param name="sid">The ID of the Discord guild.</param>
        /// <param name="uid">The ID of the Discord user.</param>
        /// <remarks>The warnings will be empty if the user has none. This overload returns notices and warnings.</remarks>
        /// <returns>The guild settings, <see langword="null"/> if for some reason the guild doesn't exist in the database.</returns>
        public async Task<GuildConfigEntity> GetGuildWithWarningsAsync(ulong sid, ulong uid)
        {
            return await base.Table
                .Include(x => x.WarnRel.Where(x => x.UserId == uid))
                .Include(x => x.WarnPunishRel)
                .FirstOrDefaultAsync(x => x.GuildId == sid);
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
        /// <param name="guild">A guild database entity.</param>
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
