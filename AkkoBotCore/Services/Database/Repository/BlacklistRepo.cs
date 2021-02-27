using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using ConcurrentCollections;
using DSharpPlus.CommandsNext;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Repository
{
    public class BlacklistRepo : DbRepository<BlacklistEntity>
    {
        private readonly AkkoDbContext _db;
        private readonly ConcurrentHashSet<ulong> _cache;

        public BlacklistRepo(AkkoDbContext db, IDbCacher dbCacher) : base(db)
        {
            _db = db;
            _cache = dbCacher.Blacklist;
        }

        /// <summary>
        /// Checks if the provided ID is blacklisted.
        /// </summary>
        /// <param name="id">ID of a user, channel or guild.</param>
        /// <returns><see langword="true"/> if the ID is blacklisted, <see langword="false"/> otherwise.</returns>
        public bool IsBlacklisted(ulong id)
            => _cache.Contains(id);

        /// <summary>
        /// Checks if the command comes from a blacklisted context.
        /// </summary>
        /// <param name="id">Context of the command.</param>
        /// <returns><see langword="true"/> if it's blacklisted, <see langword="false"/> otherwise.</returns>
        public bool IsBlacklisted(CommandContext context)
        {
            return _cache.Contains(context.User.Id)
                || _cache.Contains(context.Channel.Id)
                || _cache.Contains(context.Guild?.Id ?? default); // This will cause dms to always fail if 0 is in the blacklist.
        }

        /// <summary>
        /// Tracks multiple blacklist entries to be added to the database and adds them to the cache.
        /// </summary>
        /// <param name="entries">A collection of blacklist entries.</param>
        /// <returns>The amount of entries that have been added.</returns>
        public int TryCreateRange(IEnumerable<BlacklistEntity> entries)
        {
            foreach (var entry in entries)
                _cache.Add(entry.ContextId);

            var uniqueEntries = base.GetAllSync()
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
        public int TryRemoveRange(IEnumerable<BlacklistEntity> entries)
        {
            foreach (var entry in entries)
                _cache.TryRemove(entry.ContextId);

            var presentEntries = base.GetAllSync()
                .IntersectBy(entries, x => x.ContextId)
                .ToArray();

            base.DeleteRange(presentEntries);

            return presentEntries.Length;
        }

        /// <summary>
        /// Adds a blacklist entry to the database.
        /// </summary>
        /// <param name="value">The specified blacklist entry.</param>
        /// <returns><see langword="true"/> if the entry got added to the database or to the cache, <see langword="false"/> otherwise.</returns>
        public async Task<bool> TryCreateAsync(BlacklistEntity value)
        {
            await _db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO blacklist(context_id, type, name, date_added, reason) " +
                $"VALUES({value.ContextId}, {(int)value.Type}, '{value.Name}', '{value.DateAdded:O}', '{value.Reason}') " +
                @"ON CONFLICT (context_id) " +
                @"DO NOTHING;"
            );

            return _cache.Add(value.ContextId);
        }

        /// <summary>
        /// Removes a blacklist entry from the database.
        /// </summary>
        /// <param name="id">The specified blacklist ID.</param>
        /// <returns><see langword="true"/> if the entry got removed from the database or from the cache, <see langword="false"/> otherwise.</returns>
        public async Task<bool> TryRemoveAsync(ulong id)
        {
            if (!_cache.Contains(id))
                return false;

            await _db.Database.ExecuteSqlRawAsync($"DELETE FROM blacklist WHERE context_id = {id};");
            return _cache.TryRemove(id);
        }

        /// <summary>
        /// Removes all blacklist entries from the database.
        /// </summary>
        /// <returns>The amount of rows removed from the database.</returns>
        public async Task<int> ClearAsync()
        {
            var rows = await _db.Database.ExecuteSqlRawAsync("DELETE FROM blacklist;");
            _cache.Clear();

            return rows;
        }
    }
}