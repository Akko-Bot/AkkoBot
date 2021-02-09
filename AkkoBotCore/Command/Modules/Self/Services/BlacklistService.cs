using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AkkoBot.Command.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus.CommandsNext;

namespace AkkoBot.Command.Modules.Self.Services
{
    public class BlacklistService : ICommandService
    {
        private readonly IServiceProvider _services;

        public BlacklistService(IServiceProvider services)
            => _services = services;

        /// <summary>
        /// Tries to add a blacklist entry to the database.
        /// </summary>
        /// <param name="context">The command context</param>
        /// <param name="type">Type of blacklist entry provided by the user.</param>
        /// <param name="id">ID of the entry, provided by the user.</param>
        /// <param name="reason">The reason for the blacklist.</param>
        /// <returns>
        /// A tuple with the database entry and a boolean indicating whether the operation
        /// was successful or not.
        /// </returns>
        public async Task<(BlacklistEntity, bool)> TryAddAsync(CommandContext context, BlacklistType type, ulong id, string reason)
        {
            using var scope = context.CommandsNext.Services.GetScopedService<IUnitOfWork>(out var db);

            // Generate the database entry
            var entry = new BlacklistEntity()
            {
                ContextId = id,
                Type = type,
                Name = GetBlacklistedName(context, type, id),
                Reason = reason
            };

            var success = await db.Blacklist.TryCreateAsync(entry);

            return (entry, success);
        }

        /// <summary>
        /// Adds multiple blacklist entries to the database.
        /// </summary>
        /// <param name="ids">IDs to be added.</param>
        /// <remarks>The entries will be added as <see cref="BlacklistType.Unspecified"/>.</remarks>
        /// <returns>The amount of entries that have been added to the database.</returns>
        public int AddRange(ulong[] ids)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var entries = ids.Distinct().Select(id => new BlacklistEntity()
            {
                ContextId = id,
                Type = BlacklistType.Unspecified,
                Name = null
            });

            var result = db.Blacklist.TryCreateRange(entries);
            db.SaveChanges();

            return result;
        }

        /// <summary>
        /// Removes multiple blacklist entries from the database.
        /// </summary>
        /// <param name="ids">IDs to be removed.</param>
        /// <returns>The amount of entries that have been removed to the database.</returns>
        public int RemoveRange(ulong[] ids)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var entries = ids.Distinct().Select(id => new BlacklistEntity()
            {
                ContextId = id,
                Type = BlacklistType.Unspecified,
                Name = null
            });

            var result = db.Blacklist.TryRemoveRange(entries);
            db.SaveChanges();

            return result;
        }

        /// <summary>
        /// Tries to remove a blacklist entry from the database, if it exists.
        /// </summary>
        /// <param name="id">The ID of the entry, provided by the user.</param>
        /// <returns>
        /// The entry and <see langword="true"/>, if the removal was successful, 
        /// <see langword="null"/> and <see langword="false"/> otherwise.
        /// </returns>
        public async Task<(BlacklistEntity, bool)> TryRemoveAsync(ulong id)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            if (!db.Blacklist.IsBlacklisted(id))
                return (null, false);

            var entry = await db.Blacklist.GetAsync(id);
            var success = await db.Blacklist.TryRemoveAsync(id);

            return (entry, success);
        }

        /// <summary>
        /// Gets all blacklist entries from the database that meet the criteria of the <paramref name="selector"/>.
        /// </summary>
        /// <param name="selector">Expression tree to filter the result.</param>
        /// <returns>A collection of database entries that match the criteria of <paramref name="selector"/>.</returns>
        public async Task<IEnumerable<BlacklistEntity>> GetAsync(Expression<Func<BlacklistEntity, bool>> selector)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);
            return await db.Blacklist.GetAsync(selector);
        }

        /// <summary>
        /// Gets all blacklist entries from the database.
        /// </summary>
        /// <returns>A collection of all blacklist entries.</returns>
        public async Task<IEnumerable<BlacklistEntity>> GetAllAsync()
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);
            return await db.Blacklist.GetAllAsync();
        }

        /// <summary>
        /// Removes all blacklist entries from the database.
        /// </summary>
        /// <returns>The amount of entries removed.</returns>
        public async Task<int> ClearAsync()
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);
            return await db.Blacklist.ClearAsync();
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
                        .Select(x => x.Members.Values.FirstOrDefault(u => u.Id == id))
                        .FirstOrDefault()?.GetFullname();

                case BlacklistType.Channel:
                    return context.Client.Guilds.Values
                        .Select(x => x.Channels.Values.FirstOrDefault(c => c.Id == id))
                        .FirstOrDefault()?.Name;

                case BlacklistType.Server:
                    return context.Client.Guilds.Values.FirstOrDefault(s => s.Id == id)?.Name;

                default:
                    return null;
            }
        }
    }
}