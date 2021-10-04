using AkkoCore.Commands.Abstractions;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Queries;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for manipulating <see cref="FilteredContentEntity"/> objects.
    /// </summary>
    public sealed class ContentFilterService : ICommandService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDbCache _dbCache;

        public ContentFilterService(IServiceScopeFactory scopeFactory, IDbCache dbCache)
        {
            _scopeFactory = scopeFactory;
            _dbCache = dbCache;
        }

        /// <summary>
        /// Upserts a content filter to the database for the specified guild channel.
        /// </summary>
        /// <typeparam name="T">The type of the selected data.</typeparam>
        /// <param name="server">The Discord guild.</param>
        /// <param name="channel">The Discord channel.</param>
        /// <param name="setter">A method to define which property is going to be updated.</param>
        /// <returns>The updated property.</returns>
        public async Task<T> SetContentFilterAsync<T>(DiscordGuild server, DiscordChannel channel, Func<FilteredContentEntity, T> setter)
        {
            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            if (!_dbCache.FilteredContent.TryGetValue(server.Id, out var filters))
            {
                _dbCache.FilteredContent.TryAdd(server.Id, new());
                filters = new();
            }

            var dbFilter = filters.FirstOrDefault(x => x.GuildIdFK == server.Id && x.ChannelId == channel.Id)
                ?? await db.FilteredContent.FirstOrDefaultAsync(x => x.GuildIdFK == server.Id && x.ChannelId == channel.Id)
                ?? new() { GuildIdFK = server.Id, ChannelId = channel.Id };

            // Update the database
            db.FilteredContent.Upsert(dbFilter);
            var result = setter(dbFilter);
            await db.SaveChangesAsync();

            // Update the cache
            if (dbFilter.IsActive)
                _dbCache.FilteredContent[server.Id].Add(dbFilter);
            else
                _dbCache.FilteredContent[server.Id].TryRemove(dbFilter);

            return result;
        }

        /// <summary>
        /// Removes the content filter with the specified ID from the database.
        /// </summary>
        /// <param name="server">The Discord guild.</param>
        /// <param name="id">The database ID of the filter.</param>
        /// <returns><see langword="true"/> if the filter was successfully removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemoveContentFilterAsync(DiscordGuild server, int id)
        {
            if (!_dbCache.FilteredContent.TryGetValue(server.Id, out var filters) || !filters.Any(x => x.Id == id))
                return false;

            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            var filter = filters.FirstOrDefault(x => x.Id == id);

            db.Remove(filter);
            var result = await db.SaveChangesAsync() is not 0;

            _dbCache.FilteredContent[server.Id].TryRemove(filter);

            return result;
        }

        /// <summary>
        /// Removes all content filters from the database for the specified Discord guild.
        /// </summary>
        /// <param name="server">The Discord guild.</param>
        /// <returns><see langword="true"/> if all filters were successfully removed, <see langword="false"/> if there was no filter to remove.</returns>
        public async Task<bool> ClearContentFiltersAsync(DiscordGuild server)
        {
            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            if (!_dbCache.FilteredContent.TryRemove(server.Id, out var filters) || filters.Count is 0)
                return false;

            db.RemoveRange(filters);
            var success = await db.SaveChangesAsync() is not 0;

            filters.Clear();

            return success;
        }

        /// <summary>
        /// Gets the content filters for the specified Discord guild.
        /// </summary>
        /// <param name="server">The Discord guild.</param>
        /// <returns>A collection of content filters.</returns>
        public IReadOnlyCollection<FilteredContentEntity> GetContentFilters(DiscordGuild server)
        {
            _dbCache.FilteredContent.TryGetValue(server.Id, out var filters);
            return filters ?? new(1, 0);
        }
    }
}