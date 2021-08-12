using AkkoCore.Commands.Abstractions;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Database.Entities;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for manipulating <see cref="FilteredWordsEntity"/> objects.
    /// </summary>
    public class WordFilterService : ICommandService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDbCache _dbCache;

        public WordFilterService(IServiceScopeFactory scopeFactory, IDbCache dbCache)
        {
            _scopeFactory = scopeFactory;
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
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            _dbCache.FilteredWords.TryGetValue(sid, out var filteredWords);

            filteredWords ??= await db.FilteredWords.FirstOrDefaultAsyncEF(x => x.GuildIdFK == sid)
                ?? new() { GuildIdFK = sid };

            var amount = filteredWords.Words.Count;

            // Add the new words to the db entry
            foreach (var word in words)
            {
                if (!filteredWords.Words.Contains(word))
                    filteredWords.Words.Add(word);
            }

            // If a word got added
            if (amount != filteredWords.Words.Count)
            {
                // Update the cache
                _dbCache.FilteredWords.AddOrUpdate(sid, filteredWords, (x, y) => filteredWords);

                // Save to the database
                db.Update(filteredWords);
                await db.SaveChangesAsync();
            }

            return amount != filteredWords.Words.Count;
        }

        /// <summary>
        /// Adds the specified IDs to the list of ignored IDs.
        /// </summary>
        /// <param name="sid">The Discord server ID.</param>
        /// <param name="ids">The IDs to be added.</param>
        /// <returns><see langword="true"/> if at least one ID was added, <see langword="false"/> otherwise.</returns>
        public async Task<bool> AddIgnoredIdsAsync(ulong sid, params ulong[] ids)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            _dbCache.FilteredWords.TryGetValue(sid, out var filteredWords);

            filteredWords ??= await db.FilteredWords.FirstOrDefaultAsyncEF(x => x.GuildIdFK == sid)
                ?? new() { GuildIdFK = sid };

            var amount = filteredWords.IgnoredIds.Count;

            foreach (var id in ids)
            {
                if (!filteredWords.IgnoredIds.Contains((long)id))
                    filteredWords.IgnoredIds.Add((long)id);
            }

            // If an ID got added
            if (amount != filteredWords.IgnoredIds.Count)
            {
                // Update the cache
                _dbCache.FilteredWords.AddOrUpdate(sid, filteredWords, (x, y) => filteredWords);

                // Save to the database
                db.Update(filteredWords);
                await db.SaveChangesAsync();
            }

            return amount != filteredWords.IgnoredIds.Count;
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
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            _dbCache.FilteredWords.TryGetValue(sid, out var filteredWords);

            filteredWords ??= await db.FilteredWords.FirstOrDefaultAsyncEF(x => x.GuildIdFK == sid)
                ?? new() { GuildIdFK = sid };

            var result = action(filteredWords);

            // Update the cache
            _dbCache.FilteredWords.AddOrUpdate(sid, filteredWords, (x, y) => filteredWords);

            // I don't know which property has been changed,
            // so I need to use EF Core here
            db.Update(filteredWords);
            await db.SaveChangesAsync();

            return result;
        }

        /// <summary>
        /// Removes filtered words from the database and cache for the specified guild.
        /// </summary>
        /// <param name="sid">The Discord server ID.</param>
        /// <param name="words">The words to be removed.</param>
        /// <returns><see langword="true"/> if at least one word got removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemoveFilteredWordsAsync(ulong sid, params string[] words)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            if (!_dbCache.FilteredWords.TryGetValue(sid, out var filteredWords))
                return false;

            var amount = filteredWords.Words.Count;

            foreach (var word in words)
            {
                filteredWords.Words.Remove(word);

                if (filteredWords.Words.Count is 0)
                    _dbCache.FilteredWords.TryRemove(sid, out _);
            }

            await db.FilteredWords.UpdateAsync(
                x => x.Id == filteredWords.Id,
                _ => new FilteredWordsEntity() { Words = filteredWords.Words }
            );

            return amount != filteredWords.Words.Count;
        }

        /// <summary>
        /// Removes all filtered words from the database and cache for the specified guild.
        /// </summary>
        /// <param name="sid">The Discord server ID.</param>
        /// <returns><see langword="true"/> if at least one word got removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> ClearFilteredWordsAsync(ulong sid)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            if (!_dbCache.FilteredWords.TryGetValue(sid, out var filteredWords))
                return false;

            var amount = filteredWords.Words.Count;

            // Remove the cached words
            filteredWords.Words.Clear();
            _dbCache.FilteredWords.TryRemove(sid, out _);

            await db.FilteredWords.UpdateAsync(
                x => x.Id == filteredWords.Id,
                _ => new FilteredWordsEntity() { Words = filteredWords.Words }
            );

            return amount != filteredWords.Words.Count;
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