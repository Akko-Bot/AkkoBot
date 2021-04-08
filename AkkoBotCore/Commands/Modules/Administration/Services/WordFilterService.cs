using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for manipulating <see cref="FilteredWordsEntity"/> objects.
    /// </summary>
    public class WordFilterService : AkkoCommandService
    {
        private readonly IServiceProvider _services;

        public WordFilterService(IServiceProvider services) : base(services)
            => _services = services;

        /// <summary>
        /// Adds filtered words to the database and cache for the specified guild.
        /// </summary>
        /// <param name="sid">The Discord server ID.</param>
        /// <param name="words">The words to be added.</param>
        public async Task AddFilteredWordsAsync(ulong sid, params string[] words)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var dbEntry = await db.GuildConfig.GetGuildWithFilteredWordsAsync(sid);

            // Add the new words to the db entry
            foreach (var word in words)
            {
                if (!dbEntry.FilteredWordsRel.Words.Contains(word))
                    dbEntry.FilteredWordsRel.Words.Add(word);
            }

            // Update the cache
            db.GuildConfig.FilteredWordsCache.AddOrUpdate(sid, dbEntry.FilteredWordsRel, (x, y) => dbEntry.FilteredWordsRel);

            // Save to the database
            db.GuildConfig.Update(dbEntry);
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Adds the specified IDs to the list of ignored IDs.
        /// </summary>
        /// <param name="sid">The Discord server ID.</param>
        /// <param name="ids">The IDs to be added.</param>
        /// <returns><see langword="true"/> if at least one ID was added, <see langword="false"/> otherwise.</returns>
        public async Task<bool> AddIgnoredIdsAsync(ulong sid, params ulong[] ids)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var dbEntry = await db.GuildConfig.GetGuildWithFilteredWordsAsync(sid);
            var amount = dbEntry.FilteredWordsRel.IgnoredIds.Count;

            foreach (var id in ids)
            {
                if (!dbEntry.FilteredWordsRel.IgnoredIds.Contains((long)id))
                    dbEntry.FilteredWordsRel.IgnoredIds.Add((long)id);
            }

            // Update the cache
            db.GuildConfig.FilteredWordsCache.AddOrUpdate(sid, dbEntry.FilteredWordsRel, (x, y) => dbEntry.FilteredWordsRel);

            // Save to the database
            db.GuildConfig.Update(dbEntry);
            await db.SaveChangesAsync();

            return !(amount == dbEntry.FilteredWordsRel.IgnoredIds.Count);
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
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var dbEntry = await db.GuildConfig.GetGuildWithFilteredWordsAsync(sid);
            var result = action(dbEntry.FilteredWordsRel);

            // Update the cache
            db.GuildConfig.FilteredWordsCache.AddOrUpdate(sid, dbEntry.FilteredWordsRel, (x, y) => dbEntry.FilteredWordsRel);

            db.GuildConfig.Update(dbEntry);
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
            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();

            if (!db.GuildConfig.FilteredWordsCache.TryGetValue(sid, out _))
                return false;

            var dbEntry = await db.GuildConfig.GetGuildWithFilteredWordsAsync(sid);

            foreach (var word in words)
            {
                dbEntry.FilteredWordsRel.Words.Remove(word);                // Remove the word from the entry
                db.GuildConfig.FilteredWordsCache[sid].Words.Remove(word);  // Remove the word from the cache

                if (db.GuildConfig.FilteredWordsCache[sid].Words.Count == 0)
                    db.GuildConfig.FilteredWordsCache.TryRemove(sid, out _);
            }

            db.GuildConfig.Update(dbEntry);
            return (await db.SaveChangesAsync()) is not 0;
        }

        /// <summary>
        /// Removes all filtered words from the database and cache for the specified guild.
        /// </summary>
        /// <param name="sid">The Discord server ID.</param>
        /// <returns><see langword="true"/> if at least one word got removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> ClearFilteredWordsAsync(ulong sid)
        {
            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();

            if (!db.GuildConfig.FilteredWordsCache.TryGetValue(sid, out _))
                return false;

            var dbEntry = await db.GuildConfig.GetGuildWithFilteredWordsAsync(sid);

            // Remove the words from the entry
            dbEntry.FilteredWordsRel.Words.Clear();

            // Remove the cached list
            db.GuildConfig.FilteredWordsCache[sid].Words.Clear();
            db.GuildConfig.FilteredWordsCache.TryRemove(sid, out _);

            db.GuildConfig.Update(dbEntry);
            return (await db.SaveChangesAsync()) is not 0;
        }

        /// <summary>
        /// Gets all filtered words for the specified guild.
        /// </summary>
        /// <param name="sid">The Discord server ID.</param>
        /// <returns>The filtered words, <see langword="null"/> if the Discord server has no entry for filtered words.</returns>
        public FilteredWordsEntity GetFilteredWords(ulong sid)
        {
            _services.GetService<IDbCacher>().FilteredWords.TryGetValue(sid, out var dbentry);
            return dbentry;
        }
    }
}