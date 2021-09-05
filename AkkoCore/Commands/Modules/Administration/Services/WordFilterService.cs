using AkkoCore.Commands.Abstractions;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Queries;
using Microsoft.EntityFrameworkCore;
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
        /// Updates the filtered word settings for the specified guild.
        /// </summary>
        /// <typeparam name="T">The type of the setting that is being altered.</typeparam>
        /// <param name="sid">The Discord server ID.</param>
        /// <param name="setter">Method to change the settings.</param>
        /// <returns>The setting returned by <paramref name="setter"/>.</returns>
        public async Task<T> SetWordFilterAsync<T>(ulong sid, Func<FilteredWordsEntity, T> setter)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            if (!_dbCache.FilteredWords.TryGetValue(sid, out var filteredWords))
                filteredWords = await db.FilteredWords.FirstOrDefaultAsync(x => x.GuildIdFK == sid) ?? new() { GuildIdFK = sid };

            db.FilteredWords.Upsert(filteredWords);
            var result = setter(filteredWords);

            // Update the database
            await db.SaveChangesAsync();

            // Update the cache
            if (filteredWords.Words.Count is 0)
                _dbCache.FilteredWords.TryRemove(sid, out _);
            else
                _dbCache.FilteredWords.AddOrUpdate(sid, filteredWords, (_, _) => filteredWords);

            return result;
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