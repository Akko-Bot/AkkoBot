﻿using AkkoBot.Services.Database.Entities;
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

        public GuildConfigRepo(AkkoDbContext db, AkkoDbCacher dbCacher) : base(db)
        {
            _db = db;
            _botConfig = dbCacher.BotConfig;
            Cache = dbCacher.Guilds;
        }

        /// <summary>
        /// Gets the prefix of the specified Discord guild.
        /// </summary>
        /// <param name="sid">The ID of the Discord guild.</param>
        /// <returns>The prefix for the Discord guild.</returns>
        public async Task<string> GetPrefixAsync(ulong sid)
            => (await GetGuildAsync(sid)).Prefix;

        /// <summary>
        /// Gets the settings of the specified Discord guild.
        /// </summary>
        /// <param name="sid">The ID of the Discord guild.</param>
        /// <returns></returns>
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
        /// <returns></returns>
        public async Task TryCreateAsync(DiscordGuild guild)
        {
            var dGuild = new GuildConfigEntity(guild);

            // Add to the database
            await _db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO guild_configs(guild_id, prefix, use_embed, ok_color, error_color) " +
                $"VALUES({dGuild.GuildId}, '{_botConfig.DefaultPrefix}', {dGuild.UseEmbed}, '{dGuild.OkColor}', '{dGuild.ErrorColor}') " +
                @"ON CONFLICT (guild_id) " +
                @"DO NOTHING;"
            );

            // Add to the cache
            Cache.TryAdd(dGuild.GuildId, dGuild);
        }

        /// <summary>
        /// Upserts an entry for the specified guild into the database.
        /// </summary>
        /// <param name="guild">The ID of the Discord guild.</param>
        /// <returns></returns>
        public async Task CreateOrUpdateAsync(GuildConfigEntity guild)
        {
            await _db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO discord_users(guild_id, prefix, use_embed, ok_color, error_color) " +
                $"VALUES({guild.GuildId}, '{guild.Prefix}', {guild.UseEmbed}, '{guild.OkColor}', '{guild.ErrorColor}') " +
                @"ON CONFLICT (guild_id) " +
                @"DO UPDATE " +
                @"SET " +
                $"prefix = '{guild.Prefix}', use_embed = '{guild.UseEmbed}', " +
                $"ok_color = '{guild.OkColor}', error_color = '{guild.ErrorColor}';"
            );

            // Update the cache
            if (!Cache.TryAdd(guild.GuildId, guild))
                Cache.TryUpdate(guild.GuildId, guild, Cache[guild.GuildId]);
        }
    }
}
