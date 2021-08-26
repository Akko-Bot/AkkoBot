using AkkoCore.Commands.Abstractions;
using AkkoCore.Extensions;
using AkkoCore.Services;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using LinqToDB;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Utilities.Services
{
    /// <summary>
    /// Groups utility methods for retrieving and manipulating <see cref="TagEntity"/> objects.
    /// </summary>
    public class TagsService : ICommandService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDbCache _dbCache;

        public TagsService(IServiceScopeFactory scopeFactory, IDbCache dbCache)
        {
            _scopeFactory = scopeFactory;
            _dbCache = dbCache;
        }

        /// <summary>
        /// Adds a tag to the database.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="trigger">The tag's trigger.</param>
        /// <param name="response">The tag's response.</param>
        /// <param name="isEmoji"><see langword="true"/> if the tag should be added as a emoji tag, <see langword="false"/> is it should be added as a text tag.</param>
        /// <returns><see langword="true"/> if the tag was successfully added, <see langword="false"/> otherwise.</returns>
        public async Task<bool> AddTagAsync(CommandContext context, string trigger, string response, bool isEmoji)
        {
            if (string.IsNullOrWhiteSpace(trigger) || string.IsNullOrWhiteSpace(response)
                || (context.Guild is null && !GeneralService.IsOwner(context, context.User.Id)))
                return false;

            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            var dbTag = new TagEntity()
            {
                GuildIdFK = context.Guild?.Id,
                AuthorId = context.User.Id,
                Trigger = trigger,
                Response = response,
                IsEmoji = isEmoji
            };

            db.Add(dbTag);
            var result = await db.SaveChangesAsync() is not 0;

            if (!_dbCache.Tags.TryGetValue(context.Guild?.Id ?? default, out var tags))
            {
                tags = new();
                _dbCache.Tags.TryAdd(context.Guild?.Id ?? default, tags);
            }

            tags.Add(dbTag);

            return result;
        }

        /// <summary>
        /// Removes a tag from the database.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="id">The ID of the database tag.</param>
        /// <returns><see langword="true"/> if the tag was successfully removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemoveTagAsync(CommandContext context, int id)
        {
            if (!_dbCache.Tags.TryGetValue(context.Guild?.Id ?? default, out var tags))
                return false;

            var dbTag = tags.FirstOrDefault(x => x.Id == id);

            // Require bot ownership for global tags
            // Require user to be the tag's author or a higher admin for guild tags
            if (dbTag is null || (context.Guild is null && !GeneralService.IsOwner(context, context.User.Id))
                || !(dbTag.AuthorId == context.User.Id || context.Member?.Roles.Any(x => x.Permissions.HasPermission(Permissions.ManageGuild)) is true))
                return false;

            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            await db.Tags.DeleteAsync(x => x.Id == id);

            tags.TryRemove(dbTag);

            if (tags.Count is 0)
                _dbCache.Tags.TryRemove(context.Guild?.Id ?? default, out _);

            return true;
        }

        /// <summary>
        /// Removes a tag from the database.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="predicate">A method defining what tags in the current context should be removed.</param>
        /// <returns>The amount of removed tags.</returns>
        public async Task<int> RemoveTagAsync(CommandContext context, Expression<Func<TagEntity, bool>> predicate = null)
        {
            if (!_dbCache.Tags.TryGetValue(context.Guild?.Id ?? default, out var tags))
                return 0;

            predicate ??= x => true;

            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            var result = await db.Tags.Where(x => x.GuildIdFK == ((context.Guild == null) ? null : context.Guild.Id))
                .DeleteAsync(predicate);

            // Update the cache
            foreach (var tag in tags.Where(predicate.Compile()))
                tags.TryRemove(tag);

            if (tags.Count is 0)
                _dbCache.Tags.TryRemove(context.Guild?.Id ?? default, out _);

            return result;
        }

        /// <summary>
        /// Removes all tags from the database that haven't been used over <paramref name="time"/>.
        /// </summary>
        /// <param name="time">Time to remove the tags since they were last used.</param>
        /// <returns>The amount of removed tags.</returns>
        public async Task<int> RemoveOldTagsAsync(TimeSpan time)
        {
            var tags = _dbCache.Tags.Values
                .SelectMany(x => x)
                .Where(x => DateTimeOffset.Now.Subtract(x.LastDayUsed) > time);

            // Update the cache
            foreach (var tag in tags)
            {
                _dbCache.Tags.TryGetValue(tag.GuildIdFK ?? default, out var cachedTags);
                cachedTags.TryRemove(tag);

                if (cachedTags.Count is 0)
                    _dbCache.Tags.TryRemove(tag.GuildIdFK ?? default, out _);
            }

            if (!tags.Any())
                return 0;

            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            return await db.Tags.DeleteAsync(x => DateTimeOffset.Now - x.LastDayUsed > time);
        }

        /// <summary>
        /// Sets the specified tag setting.
        /// </summary>
        /// <typeparam name="T">The type of the setting.</typeparam>
        /// <param name="server">The Discord guild or <see langword="null"/> if the tag is global.</param>
        /// <param name="id">The database ID of the tag.</param>
        /// <param name="setter">A method to set the property.</param>
        /// <returns>The modified setting.</returns>
        public async Task<T> SetPropertyAsync<T>(DiscordGuild server, int id, Func<TagEntity, T> setter)
        {
            if (!GetCachedTag(server?.Id, id, out var dbTag))
                return default;

            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            db.Tags.Attach(dbTag);
            var result = setter(dbTag);
            await db.SaveChangesAsync();

            return result;
        }

        /// <summary>
        /// Gets the cached tags for the specified guild.
        /// </summary>
        /// <param name="sid">The ID of the Discord guild. <see langword="null"/> to get the global tags.</param>
        /// <returns>The collection of tags.</returns>
        public IReadOnlyCollection<TagEntity> GetTags(ulong? sid)
        {
            _dbCache.Tags.TryGetValue(sid ?? default, out var tags);
            return tags ?? new(1, 0);
        }

        /// <summary>
        /// Gets a cached tag with the specified ID.
        /// </summary>
        /// <param name="sid">The ID of the Discord guild or <see langword="null"/> for global tags.</param>
        /// <param name="id">The database ID of the tag.</param>
        /// <param name="dbTag">The tag if found, <see langword="null"/> if it was not found.</param>
        /// <returns><see langword="true"/> if the tag was found, <see langword="false"/> otherwise.</returns>
        private bool GetCachedTag(ulong? sid, int id, out TagEntity dbTag)
        {
            _dbCache.Tags.TryGetValue(sid ?? default, out var tags);
            dbTag = tags?.FirstOrDefault(x => x.Id == id);

            return dbTag is not null;
        }
    }
}