using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Database.Queries;
using DSharpPlus.CommandsNext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Self.Services
{
    /// <summary>
    /// Groups utility methods for retrieving and manipulating <see cref="BlacklistEntity"/> objects.
    /// </summary>
    public class BlacklistService : AkkoCommandService
    {
        private readonly IServiceProvider _services;
        private readonly IDbCache _dbCache;

        public BlacklistService(IServiceProvider services, IDbCache dbCache) : base(services)
        {
            _services = services;
            _dbCache = dbCache;
        }

        /// <summary>
        /// Saves a blacklist entry to the database.
        /// </summary>
        /// <param name="context">The command context</param>
        /// <param name="type">Type of blacklist entry provided by the user.</param>
        /// <param name="id">ID of the entry, provided by the user.</param>
        /// <param name="reason">The reason for the blacklist.</param>
        /// <returns>
        /// A tuple with the database entry and a boolean indicating whether the entry was
        /// added (<see langword="true"/>) or updated (<see langword="false"/>).
        /// </returns>
        public async Task<(BlacklistEntity, bool)> AddOrUpdateAsync(CommandContext context, BlacklistType type, ulong id, string reason)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            // Generate the database entry
            var entry = new BlacklistEntity()
            {
                ContextId = id,
                Type = type,
                Name = GetBlacklistedName(context, type, id),
                Reason = reason
            };

            db.Upsert(entry);
            await db.SaveChangesAsync();

            return (entry, _dbCache.Blacklist.Add(id));
        }

        /// <summary>
        /// Adds multiple blacklist entries to the database.
        /// </summary>
        /// <param name="ids">IDs to be added.</param>
        /// <remarks>The entries will be added as <see cref="BlacklistType.Unspecified"/>.</remarks>
        /// <returns>The amount of entries that have been added to the database.</returns>
        public async Task<int> AddRangeAsync(ulong[] ids)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var newEntries = ids.Distinct().Select(id => new BlacklistEntity()
            {
                ContextId = id,
                Type = BlacklistType.Unspecified,
                Name = null
            });

            foreach (var blacklist in newEntries)
                _dbCache.Blacklist.Add(blacklist.ContextId);

            db.AddRange(newEntries);
            return await db.SaveChangesAsync();
        }

        /// <summary>
        /// Removes multiple blacklist entries from the database.
        /// </summary>
        /// <param name="ids">IDs to be removed.</param>
        /// <returns>The amount of entries that have been removed from the database.</returns>
        public async Task<int> RemoveRangeAsync(ulong[] ids)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);
            var dbEntries = (await db.Blacklist.Fetch().ToArrayAsync())
                .Where(x => ids.Contains(x.ContextId));

            foreach (var blacklist in dbEntries)
                _dbCache.Blacklist.TryRemove(blacklist.ContextId);

            db.RemoveRange(dbEntries);
            return await db.SaveChangesAsync();
        }

        /// <summary>
        /// Tries to remove a blacklist entry from the database, if it exists.
        /// </summary>
        /// <param name="contextId">The context (user/channel/server) ID of the entry.</param>
        /// <returns>
        /// The entry and <see langword="true"/>, if the removal was successful,
        /// <see langword="null"/> and <see langword="false"/> otherwise.
        /// </returns>
        public async Task<(BlacklistEntity, bool)> TryRemoveAsync(ulong contextId)
        {
            if (!_dbCache.Blacklist.Contains(contextId))
                return (null, false);

            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var entry = await db.Blacklist.FirstOrDefaultAsync(x => x.ContextId == contextId);
            _dbCache.Blacklist.TryRemove(contextId);
            db.Remove(entry);

            return (entry, await db.SaveChangesAsync() is not 0);
        }

        /// <summary>
        /// Gets all blacklist entries from the database that meet the criteria of the <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">Expression tree to filter the result.</param>
        /// <remarks>If <paramref name="predicate"/> is <see langword="null"/>, it gets all blacklist entries.</remarks>
        /// <returns>A collection of database entries that match the criteria of <paramref name="predicate"/>.</returns>
        public async Task<BlacklistEntity[]> GetAsync(Expression<Func<BlacklistEntity, bool>> predicate = null)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);
            return await db.Blacklist.Fetch(predicate).ToArrayAsync();
        }

        /// <summary>
        /// Removes all blacklist entries from the database.
        /// </summary>
        /// <returns>The amount of entries removed.</returns>
        public async Task<int> ClearAsync()
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);
            var blacklist = _dbCache.Blacklist;

            db.RemoveRange(_dbCache.Blacklist);
            var result = await db.SaveChangesAsync();

            blacklist.Clear();
            return result;
        }

        /// <summary>
        /// Gets the name of the blacklist entity.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="blType">The type of the blacklist.</param>
        /// <param name="id">The ID provided by the user.</param>
        /// <returns>The name of the entity if found, <see langword="null"/> otherwise.</returns>
        private string GetBlacklistedName(CommandContext context, BlacklistType blType, ulong id)
        {
            switch (blType)
            {
                case BlacklistType.User:
                    return context.Client.Guilds.Values
                        .FirstOrDefault(x => x.Members.Values.Any(u => u.Id == id))
                        .Members.Values.FirstOrDefault(u => u.Id == id)
                        ?.GetFullname();

                case BlacklistType.Channel:
                    return context.Client.Guilds.Values
                        .FirstOrDefault(x => x.Channels.Values.Any(c => c.Id == id))
                        ?.Channels.Values.FirstOrDefault(c => c.Id == id)?.Name;

                case BlacklistType.Server:
                    return context.Client.Guilds.Values.FirstOrDefault(s => s.Id == id)?.Name;

                default:
                    return null;
            }
        }
    }
}