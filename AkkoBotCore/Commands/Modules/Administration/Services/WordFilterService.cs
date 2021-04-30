using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for manipulating <see cref="FilteredWordsEntity"/> objects.
    /// </summary>
    public class WordFilterService : ICommandService
    {
        private readonly IServiceProvider _services;
        private readonly IDbCache _dbCache;

        public WordFilterService(IServiceProvider services, IDbCache dbCache)
        {
            _services = services;
            _dbCache = dbCache;
        }

        /// <summary>
        /// Adds filtered words to the database and cache for the specified guild.
        /// </summary>
        /// <param name="sid">The Discord server ID.</param>
        /// <param name="words">The words to be added.</param>
        /// <returns><see langword="true"/> if at least one word got added, <see langword="false"/> otherwise.</returns>
        public async Task<bool> AddFilteredWordsAsync(ulong sid, params string[] words)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var filteredWords = await db.FilteredWords.FirstOrDefaultAsync(x => x.GuildIdFK == sid)
                ?? new() { GuildIdFK = sid };

            // Add the new words to the db entry
            foreach (var word in words)
            {
                if (!filteredWords.Words.Contains(word))
                    filteredWords.Words.Add(word);
            }

            // Update the cache
            _dbCache.FilteredWords.AddOrUpdate(sid, filteredWords, (x, y) => filteredWords);

            // Save to the database
            db.Update(filteredWords);
            return await db.SaveChangesAsync() is not 0;
        }

        /// <summary>
        /// Adds the specified IDs to the list of ignored IDs.
        /// </summary>
        /// <param name="sid">The Discord server ID.</param>
        /// <param name="ids">The IDs to be added.</param>
        /// <returns><see langword="true"/> if at least one ID was added, <see langword="false"/> otherwise.</returns>
        public async Task<bool> AddIgnoredIdsAsync(ulong sid, params ulong[] ids)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var filteredWords = await db.FilteredWords.FirstOrDefaultAsync(x => x.GuildIdFK == sid);
            var amount = filteredWords.IgnoredIds.Count;

            foreach (var id in ids)
            {
                if (!filteredWords.IgnoredIds.Contains((long)id))
                    filteredWords.IgnoredIds.Add((long)id);
            }

            // Update the cache
            _dbCache.FilteredWords.AddOrUpdate(sid, filteredWords, (x, y) => filteredWords);

            // Save to the database
            db.Update(filteredWords);
            await db.SaveChangesAsync();

            return !(amount == filteredWords.IgnoredIds.Count);
        }

        /// <summary>
        /// Updates the filtered word settings for the specified guild.
        /// </summary>
        /// <typeparam name="T">The type of the setting that is being altered.</typeparam>
        /// <param name="sid">The Discord server ID.</param>
        /// <param name="action">Method to change the settings.</param>
        /// <returns>The setting returned by <paramref name="action"/>.</returns>
        public async Task<T> SetWordFilterSettingsAsync<T>(ulong sid, Func<FilteredWordsEntity, T> action)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var filteredWords = await db.FilteredWords.FirstOrDefaultAsync(x => x.GuildIdFK == sid);
            var result = action(filteredWords);

            // Update the cache
            _dbCache.FilteredWords.AddOrUpdate(sid, filteredWords, (x, y) => filteredWords);

            db.Update(filteredWords);
            await db.SaveChangesAsync();

            return result;
        }

        /// <summary>
        /// Removes filtered words from the database and cache for the specified guild.
        /// </summary>
        /// <param name="sid">The Discord server ID.</param>
        /// <param name="words">The words to be removed.</param>
        /// <returns><see langword="true"/> if at least one word got added, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemoveFilteredWordsAsync(ulong sid, params string[] words)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            if (!_dbCache.FilteredWords.TryGetValue(sid, out _))
                return false;

            var filteredWords = await db.FilteredWords.FirstOrDefaultAsync(x => x.GuildIdFK == sid);

            foreach (var word in words)
            {
                filteredWords.Words.Remove(word);                         // Remove the word from the entry
                _dbCache.FilteredWords[sid].Words.Remove(word);     // Remove the word from the cache

                if (_dbCache.FilteredWords[sid].Words.Count == 0)
                    _dbCache.FilteredWords.TryRemove(sid, out _);
            }

            db.Update(filteredWords);
            return await db.SaveChangesAsync() is not 0;
        }

        /// <summary>
        /// Removes all filtered words from the database and cache for the specified guild.
        /// </summary>
        /// <param name="sid">The Discord server ID.</param>
        /// <returns><see langword="true"/> if at least one word got removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> ClearFilteredWordsAsync(ulong sid)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            if (!_dbCache.FilteredWords.TryGetValue(sid, out _))
                return false;

            var filteredWords = await db.FilteredWords.FirstOrDefaultAsync(x => x.GuildIdFK == sid);

            // Remove the words from the entry
            filteredWords.Words.Clear();

            // Remove the cached list
            _dbCache.FilteredWords[sid].Words.Clear();
            _dbCache.FilteredWords.TryRemove(sid, out _);

            db.Update(filteredWords);
            return (await db.SaveChangesAsync()) is not 0;
        }

        /// <summary>
        /// Gets all filtered words for the specified guild.
        /// </summary>
        /// <param name="sid">The Discord server ID.</param>
        /// <returns>The filtered words, <see langword="null"/> if the Discord server has no entry for filtered words.</returns>
        public FilteredWordsEntity GetFilteredWords(ulong sid)
        {
            _dbCache.FilteredWords.TryGetValue(sid, out var dbentry);
            return dbentry;
        }
    }
}