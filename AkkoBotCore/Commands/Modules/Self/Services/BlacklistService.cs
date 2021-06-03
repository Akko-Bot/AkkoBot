using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Database.Queries;
using DSharpPlus.CommandsNext;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore;
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
    public class BlacklistService : ICommandService
    {
        private readonly IServiceProvider _services;
        private readonly IDbCache _dbCache;

        /// <summary>
        /// Checks whether there are entries in the blacklist.
        /// </summary>
        public bool HasBlacklists => _dbCache.Blacklist.Count is not 0;

        public BlacklistService(IServiceProvider services, IDbCache dbCache)
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
        public async Task<(BlacklistEntity, bool)> AddBlacklistAsync(CommandContext context, BlacklistType type, ulong id, string reason)
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
        public async Task<int> AddBlacklistsAsync(ulong[] ids)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var newEntries = ids.Distinct()
                .Select(id => new BlacklistEntity() { ContextId = id, Type = BlacklistType.Unspecified });

            foreach (var blacklist in newEntries)
                _dbCache.Blacklist.Add(blacklist.ContextId);

            return (int)(await db.BulkCopyAsync(newEntries)).RowsCopied;
        }

        /// <summary>
        /// Removes multiple blacklist entries from the database.
        /// </summary>
        /// <param name="ids">IDs to be removed.</param>
        /// <returns>The amount of entries that have been removed from the database.</returns>
        public async Task<int> RemoveBlacklistsAsync(ulong[] ids)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            foreach (var id in ids)
                _dbCache.Blacklist.TryRemove(id);

            return await db.Blacklist.DeleteAsync(x => ids.Contains(x.ContextId));
        }

        /// <summary>
        /// Tries to remove a blacklist entry from the database, if it exists.
        /// </summary>
        /// <param name="contextId">The context (user/channel/server) ID of the entry.</param>
        /// <returns>
        /// The entry if the removal was successful, <see langword="null"/> otherwise.
        /// </returns>
        public async Task<BlacklistEntity> RemoveBlacklistAsync(ulong contextId)
        {
            if (!_dbCache.Blacklist.Contains(contextId))
                return null;

            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var entry = await db.Blacklist.FirstOrDefaultAsync(x => x.ContextId == contextId);

            _dbCache.Blacklist.TryRemove(contextId);
            db.Remove(entry);

            await db.SaveChangesAsync();

            return entry;
        }

        /// <summary>
        /// Removes all blacklist entries from the database.
        /// </summary>
        /// <returns>The amount of entries removed.</returns>
        public async Task<int> ClearBlacklistsAsync()
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            _dbCache.Blacklist.Clear();
            return await db.Blacklist.DeleteAsync();
        }

        /// <summary>
        /// Gets all blacklist entries from the database that meet the criteria of the <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">Expression tree to filter the result.</param>
        /// <remarks>If <paramref name="predicate"/> is <see langword="null"/>, it gets all blacklist entries.</remarks>
        /// <returns>A collection of blacklist entries that match the criteria of <paramref name="predicate"/>.</returns>
        public async Task<IReadOnlyCollection<BlacklistEntity>> GetBlacklistAsync(Expression<Func<BlacklistEntity, bool>> predicate = null)
            => await GetBlacklistAsync(predicate, x => x);

        /// <summary>
        /// Gets a collection of <typeparamref name="T"/> from the blacklist entries in the database that meet the criteria of the <paramref name="predicate"/>.
        /// </summary>
        /// <typeparam name="T">The selected returning type.</typeparam>
        /// <param name="predicate">Expression tree to filter the result.</param>
        /// <param name="selector">Expression tree to select the columns to be returned.</param>
        /// <remarks>If <paramref name="predicate"/> is <see langword="null"/>, it gets all blacklist entries.</remarks>
        /// <returns>A collection of <typeparamref name="T"/> whose entries match the criteria of <paramref name="predicate"/>.</returns>
        /// <exception cref="ArgumentNullException">Occurs when <paramref name="selector"/> is <see langword="null"/>.</exception>
        public async Task<IReadOnlyCollection<T>> GetBlacklistAsync<T>(Expression<Func<BlacklistEntity, bool>> predicate, Expression<Func<BlacklistEntity, T>> selector)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            return await db.Blacklist.Fetch(predicate)
                .Select(selector)
                .ToArrayAsync();
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