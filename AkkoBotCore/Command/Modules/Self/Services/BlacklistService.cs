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
using Microsoft.Extensions.DependencyInjection;

namespace AkkoBot.Command.Modules.Self.Services
{
    public class BlacklistService : ICommandService
    {
        /// <summary>
        /// Tries to add a blacklist entry to the database.
        /// </summary>
        /// <param name="context">The command context</param>
        /// <param name="type">Type of blacklist entry provided by the user.</param>
        /// <param name="id">ID of the entry, provided by the user.</param>
        /// <returns>
        /// A tuple with the database entry and a boolean indicating whether the operation
        /// was successful or not.
        /// </returns>
        public async Task<(BlacklistEntity, bool)> TryAddAsync(CommandContext context, string type, ulong id)
        {
            using var scope = context.CommandsNext.Services.CreateScope();
            var db = scope.ServiceProvider.GetService<IUnitOfWork>();

            // Determine the appropriate type specified by the user
            var blType = GetBlacklistType(type);

            // Generte the database entry
            var entry = new BlacklistEntity()
            {
                ContextId = id,
                Type = blType,
                Name = GetBlacklistedNameAsync(context, blType, id)
            };

            var success = await db.Blacklist.TryCreateAsync(entry);

            return (entry, success);
        }

        /// <summary>
        /// Tries to remove a blacklist entry from the database, if it exists.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="id">The ID of the entry, provided by the user.</param>
        /// <returns>
        /// The entry and <see langword="true"/>, if the removal was successful, 
        /// <see langword="null"/> and <see langword="false"/> otherwise.
        /// </returns>
        public async Task<(BlacklistEntity, bool)> TryRemoveAsync(CommandContext context, ulong id)
        {
            using var scope = context.CommandsNext.Services.CreateScope();
            var db = scope.ServiceProvider.GetService<IUnitOfWork>();

            if (!db.Blacklist.IsBlacklisted(id))
                return (null, false);

            var entry = await db.Blacklist.GetAsync(id);
            var success = await db.Blacklist.RemoveAsync(id);

            return (entry, success);
        }

        /// <summary>
        /// Gets all blacklist entries from the database that meet the criteria of the <paramref name="selector"/>.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="selector">Expression tree to filter the result.</param>
        /// <returns>A collection of database entries that match the criteria of <paramref name="selector"/>.</returns>
        public async Task<IEnumerable<BlacklistEntity>> GetAsync(CommandContext context, Expression<Func<BlacklistEntity, bool>> selector)
        {
            using var scope = context.CommandsNext.Services.CreateScope();
            var db = scope.ServiceProvider.GetService<IUnitOfWork>();

            return await db.Blacklist.GetAsync(selector);
        }

        /// <summary>
        /// Gets all blacklist entries from the database.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <returns>A collection of all blacklist entries.</returns>
        public async Task<IEnumerable<BlacklistEntity>> GetAllAsync(CommandContext context)
        {
            using var scope = context.CommandsNext.Services.CreateScope();
            var db = scope.ServiceProvider.GetService<IUnitOfWork>();

            return await db.Blacklist.GetAllAsync();
        }

        /// <summary>
        /// Removes all blacklist entries from the database.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <returns>The amount of entries removed.</returns>
        public async Task<int> ClearAsync(CommandContext context)
        {
            using var scope = context.CommandsNext.Services.CreateScope();
            var db = scope.ServiceProvider.GetService<IUnitOfWork>();

            return await db.Blacklist.ClearAsync();
        }

        /// <summary>
        /// Parses the user input into the appropriate blacklist type.
        /// </summary>
        /// <param name="type">The type provided by the user.</param>
        /// <returns>The appropriate <see cref="BlacklistType"/>.</returns>
        public BlacklistType GetBlacklistType(string type)
        {
            // Determine the appropriate type specified by the user
            return type?.ToLowerInvariant() switch
            {
                "u" or "user" => BlacklistType.User,
                "c" or "channel" => BlacklistType.Channel,
                "s" or "server" => BlacklistType.Server,
                _ => BlacklistType.Unspecified
            };
        }

        /// <summary>
        /// Gets the name of the blacklist entity.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="blType">The type of the blacklist.</param>
        /// <param name="id">The ID provided by the user.</param>
        /// <returns>The name of the entity if found, <see langword="null"/> otherwise.</returns>
        private string GetBlacklistedNameAsync(CommandContext context, BlacklistType blType, ulong id)
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