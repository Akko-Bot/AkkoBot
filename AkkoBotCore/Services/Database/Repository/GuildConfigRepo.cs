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
        /// Gets the settings of the specified Discord guild with all users that are muted on the server.
        /// </summary>
        /// <param name="sid">The ID of the Discord guild.</param>
        /// <returns>The guild settings, <see langword="null"/> if for some reason the guild doesn't exist in the database.</returns>
        public async Task<GuildConfigEntity> GetGuildWithMutesAsync(ulong sid)
        {
            return await base.Table
                .Include(x => x.MutedUserRel)
                .FirstOrDefaultAsync(x => x.GuildId == sid);
        }

        /// <summary>
        /// Creates or updates the punishment occurence for a given user in the specified server.
        /// </summary>
        /// <param name="server">The Discord guild the punishment took place.</param>
        /// <param name="userId">The Id of the punished Discord user.</param>
        /// <param name="newEntry">The new entry to be created, if there isn't one yet.</param>
        /// <remarks>Don't forget to call <see cref="IUnitOfWork.SaveChangesAsync()"/> to apply the changes.</remarks>
        /// <returns><see langword="true"/> if a new occurence was created, <see langword="false"/> if it updated.</returns>
        public async Task<bool> CreateOccurrenceAsync(DiscordGuild server, ulong userId, OccurrenceEntity newEntry)
        {
            var guildSettings = await base.Table
                .Include(x => x.OccurrenceRel.Where(x => x.UserId == userId))
                .FirstOrDefaultAsync(x => x.GuildId == server.Id);

            var occurrency = guildSettings.OccurrenceRel.FirstOrDefault(x => x.UserId == userId);
            var isCreated = false;

            if (occurrency is null)
            {
                guildSettings.OccurrenceRel.Add(newEntry);
                isCreated = true;
            }
            else
            {
                guildSettings.OccurrenceRel.Remove(occurrency);
                occurrency += newEntry;
                guildSettings.OccurrenceRel.Add(occurrency);
            }

            base.Update(guildSettings);
            return isCreated;
        }

        /// <summary>
        /// Gets the settings of the specified Discord guild with the server punishments.
        /// </summary>
        /// <param name="sid">The ID of the Discod guild.</param>
        /// <returns>The guild settings, <see langword="null"/> if for some reason the guild doesn't exist in the database.</returns>
        public async Task<GuildConfigEntity> GetGuildWithPunishmentsAsync(ulong sid)
        {
            return await base.Table
                .Include(x => x.WarnPunishRel.OrderBy(x => x.WarnAmount))
                .FirstOrDefaultAsync(x => x.GuildId == sid);
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
                .Include(x => x.OccurrenceRel.Where(x => x.UserId == uid))
                .Include(x => x.WarnRel.Where(x => x.UserId == uid && x.Type == type))
                .Include(x => x.WarnPunishRel)
                .FirstOrDefaultAsync(x => x.GuildId == sid);
        }

        /// <summary>
        /// Gets the settings of the specified Discord guild with the warnings of a specific user.
        /// </summary>
        /// <param name="sid">The ID of the Discord guild.</param>
        /// <param name="uid">The ID of the Discord user.</param>
        /// <remarks>The warnings will be empty if the user has none. This overload returns notices, warnings and occurrences.</remarks>
        /// <returns>The guild settings, <see langword="null"/> if for some reason the guild doesn't exist in the database.</returns>
        public async Task<GuildConfigEntity> GetGuildWithWarningsAsync(ulong sid, ulong uid)
        {
            return await base.Table
                .Include(x => x.OccurrenceRel.Where(x => x.UserId == uid))
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
