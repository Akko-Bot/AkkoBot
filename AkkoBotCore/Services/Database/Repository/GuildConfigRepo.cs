using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Repository
{
    public class GuildConfigRepo : DbRepository<GuildConfigEntity>
    {
        private readonly AkkoDbContext _db;
        private readonly BotConfigEntity _botConfig;
        public ConcurrentDictionary<ulong, GuildConfigEntity> Cache { get; }

        public GuildConfigRepo(AkkoDbContext db, IDbCacher dbCacher) : base(db)
        {
            _db = db;
            _botConfig = dbCacher.BotConfig;
            Cache = dbCacher.Guilds;
        }

        /// <summary>
        /// Gets the prefix of the specified Discord guild.
        /// </summary>
        /// <param name="sid">The ID of the Discord guild.</param>
        /// <returns>The prefix for the specified Discord guild.</returns>
        public async Task<string> GetPrefixAsync(ulong sid)
            => (await GetGuildAsync(sid)).Prefix;

        /// <summary>
        /// Gets the locale of the specified Discord guild.
        /// </summary>
        /// <param name="sid">The ID of the Discord guild.</param>
        /// <returns>The locale for the specified Discord guild.</returns>
        public async Task<string> GetLocaleAsync(ulong sid)
            => (await GetGuildAsync(sid)).Locale;

        /// <summary>
        /// Gets the settings of the specified Discord guild.
        /// </summary>
        /// <param name="sid">The ID of the Discord guild.</param>
        /// <returns>The guild settings.</returns>
        public async Task<GuildConfigEntity> GetGuildAsync(ulong sid)
        {
            if (Cache.ContainsKey(sid))
                return Cache[sid];
            else
            {
                var guild = await base.GetAsync(sid);
                Cache.TryAdd(guild.GuildId, guild);

                return guild;
            }
        }

        /// <summary>
        /// Adds an entry for the specified guild into the database.
        /// </summary>
        /// <param name="guild">The ID of the Discord guild.</param>
        /// <remarks>If an entry for the guild already exists, it does nothing.</remarks>
        /// <returns><see langword="true"/> if the user got added to the database or to the cache, <see langword="false"/> otherwise.</returns>
        public async Task<bool> TryCreateAsync(DiscordGuild guild)
        {
            var dGuild = new GuildConfigEntity(_botConfig) { GuildId = guild.Id };

            // Add to the database
            await _db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO guild_configs(guild_id, prefix, locale, use_embed, ok_color, error_color, date_added) " +
                $"VALUES({dGuild.GuildId}, '{_botConfig.BotPrefix}', '{dGuild.Locale}', {dGuild.UseEmbed}, '{dGuild.OkColor}', '{dGuild.ErrorColor}', '{dGuild.DateAdded:O}') " +
                @"ON CONFLICT (guild_id) " +
                @"DO NOTHING;"
            );

            // Add to the cache
            return Cache.TryAdd(dGuild.GuildId, dGuild);
        }

        /// <summary>
        /// Upserts an entry for the specified guild into the database.
        /// </summary>
        /// <param name="guild">The ID of the Discord guild.</param>
        /// <returns><see langword="true"/> if the user got added to the database or to the cache, <see langword="false"/> if it got updated.</returns>
        public async Task<bool> CreateOrUpdateAsync(GuildConfigEntity guild)
        {
            await _db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO discord_users(guild_id, prefix, locale, use_embed, ok_color, error_color, date_added) " +
                $"VALUES({guild.GuildId}, '{guild.Prefix}', '{guild.Locale}' {guild.UseEmbed}, '{guild.OkColor}', '{guild.ErrorColor}', '{guild.DateAdded:O}') " +
                @"ON CONFLICT (guild_id) " +
                @"DO UPDATE " +
                @"SET " +
                $"prefix = '{guild.Prefix}', use_embed = '{guild.UseEmbed}', " +
                $"ok_color = '{guild.OkColor}', error_color = '{guild.ErrorColor}';"
            );

            // Update the cache
            if (!Cache.TryAdd(guild.GuildId, guild))
            {
                Cache.TryUpdate(guild.GuildId, guild, Cache[guild.GuildId]);
                return false;
            }

            return true;
        }
    }
}
