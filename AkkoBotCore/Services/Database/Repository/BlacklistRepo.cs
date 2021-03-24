using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using ConcurrentCollections;
using DSharpPlus.CommandsNext;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Repository
{
    public class BlacklistRepo : DbRepository<BlacklistEntity>
    {
        public ConcurrentHashSet<ulong> Cache { get; }

        public BlacklistRepo(AkkoDbContext db, IDbCacher dbCacher) : base(db)
            => Cache = dbCacher.Blacklist;

        /// <summary>
        /// Checks if the command comes from a blacklisted context.
        /// </summary>
        /// <param name="id">Context of the command.</param>
        /// <returns><see langword="true"/> if it's blacklisted, <see langword="false"/> otherwise.</returns>
        public bool IsBlacklisted(CommandContext context)
        {
            return Cache.Contains(context.User.Id)
                || Cache.Contains(context.Channel.Id)
                || Cache.Contains(context.Guild?.Id ?? context.User.Id);
        }

        /// <summary>
        /// Tracks multiple blacklist entries to be added to the database and adds them to the cache.
        /// </summary>
        /// <param name="entries">A collection of blacklist entries.</param>
        /// <returns>The amount of entries that have been added.</returns>
        public async Task<int> TryCreateRangeAsync(IEnumerable<BlacklistEntity> entries)
        {
            foreach (var entry in entries)
                Cache.Add(entry.ContextId);

            var uniqueEntries = (await base.GetAllAsync())
                .ExceptBy(entries, x => x.ContextId)
                .ToArray();

            base.CreateRange(uniqueEntries);

            return uniqueEntries.Length;
        }

        /// <summary>
        /// Tracks multiple blacklist entries to be removed from the database and removes them from the cache.
        /// </summary>
        /// <param name="entries">A collection of blacklist entries.</param>
        /// <returns>The amount of entries that have been removed.</returns>
        public async Task<int> TryRemoveRangeAsync(IEnumerable<BlacklistEntity> entries)
        {
            foreach (var entry in entries)
                Cache.TryRemove(entry.ContextId);

            var presentEntries = (await base.GetAllAsync())
                .IntersectBy(entries, x => x.ContextId)
                .ToArray();

            base.DeleteRange(presentEntries);

            return presentEntries.Length;
        }

        /// <summary>
        /// Adds a blacklist entry to the database.
        /// </summary>
        /// <param name="newEntry">The specified blacklist entry.</param>
        /// <returns><see langword="true"/> if the entry is tracked to be added to the database, <see langword="false"/> if it's tracked for updating.</returns>
        public async Task<bool> CreateOrUpdateAsync(BlacklistEntity newEntry)
        {
            var dbEntry = await base.Table.FirstOrDefaultAsync(x => x.ContextId == newEntry.ContextId);

            if (dbEntry is null)
            {
                base.Create(newEntry);
                Cache.Add(newEntry.ContextId);

                return true;
            }
            else
            {
                dbEntry.Name = newEntry.Name;
                dbEntry.Reason = newEntry.Reason;
                dbEntry.Type = newEntry.Type;

                base.Update(dbEntry);
                return false;
            }
        }

        /// <summary>
        /// Removes a blacklist entry from the database.
        /// </summary>
        /// <param name="id">The specified blacklist ID.</param>
        /// <returns><see langword="true"/> if the entry is tracked for removal from the database, <see langword="false"/> otherwise.</returns>
        public async Task<bool> TryRemoveAsync(ulong id)
        {
            var dbEntry = await base.Table.FirstOrDefaultAsync(x => x.ContextId == id);

            if (dbEntry is not null)
                base.Delete(dbEntry);

            return Cache.TryRemove(id);
        }

        /// <summary>
        /// Removes all blacklist entries from the database.
        /// </summary>
        /// <returns>The amount of entries tracked for removal from the database.</returns>
        public async Task<int> ClearAsync()
        {
            var allEntries = await base.GetAllAsync();
            base.DeleteRange(allEntries.ToArray());

            var amount = Cache.Count;
            Cache.Clear();

            return amount;
        }
    }
}