﻿using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using ConcurrentCollections;
using DSharpPlus.CommandsNext;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for manipulating <see cref="AliasEntity"/> objects.
    /// </summary>
    public class AliasService : ICommandService
    {
        private readonly IServiceProvider _services;
        private readonly IDbCacher _dbCache;

        public AliasService(IServiceProvider services, IDbCacher dbCache)
        {
            _services = services;
            _dbCache = dbCache;
        }

        /// <summary>
        /// Saves an alias to the database.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="alias">The specified alias.</param>
        /// <param name="command">The command string, with arguments if any.</param>
        /// <returns><see langword="true"/> if the alias got successfully saved, <see langword="false"/> otherwise.</returns>
        public async Task<bool> SaveAliasAsync(CommandContext context, string alias, string command)
        {
            if (context.Guild is null && !GeneralService.IsOwner(context, context.User.Id))
                return false;

            // Sanitize the command input
            command = SanitizeCommandString(context.Prefix, command);

            // Check if the command exists
            var cmd = context.CommandsNext.FindCommand(command, out var args);

            if (cmd is null)
                return false;

            using var scope = context.Services.GetScopedService<IUnitOfWork>(out var db);

            // Save the new entry to the database
            var newEntry = new AliasEntity()
            {
                GuildId = context.Guild?.Id,
                IsDynamic = (cmd is CommandGroup c) && c.Children.Count > 1     // This is not perfect but it will do for now
                    || cmd.Overloads.Any(x => x.Arguments.Count > args.Split(' ').Length - ((string.IsNullOrWhiteSpace(args)) ? 1 : 0)),

                Alias = alias,
                Command = cmd.QualifiedName,
                Arguments = args ?? string.Empty
            };

            var guildId = newEntry.GuildId ?? default;
            var created = db.Aliases.AddOrUpdate(newEntry, newEntry.GuildId, out var dbEntry);
            await db.SaveChangesAsync();

            // Update the cache
            if (!db.Aliases.Cache.ContainsKey(guildId))
                db.Aliases.Cache.TryAdd(guildId, new ConcurrentHashSet<AliasEntity>());
            else if (!created)
                db.Aliases.Cache[guildId].TryRemove(db.Aliases.Cache[guildId].FirstOrDefault(x => x.Id == dbEntry.Id));

            return db.Aliases.Cache[guildId].Add(dbEntry);
        }

        /// <summary>
        /// Removes an alias from the database.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="alias">The specified alias.</param>
        /// <returns><see langword="true"/> if the alias was successfully removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemoveAliasAsync(CommandContext context, string alias)
        {
            if (context.Guild is null && !GeneralService.IsOwner(context, context.User.Id))
                return false;

            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            if (!db.Aliases.Cache.TryGetValue(context.Guild?.Id ?? default, out var aliases))
                return false;

            var prefix = db.GuildConfig.GetGuild(context.Guild?.Id ?? 0)?.Prefix
                ?? db.BotConfig.Cache.BotPrefix;

            var dbEntry = aliases.FirstOrDefault(x => x.Alias.Equals((x.IsDynamic) ? alias.Replace(prefix, "{p}") : alias, StringComparison.InvariantCultureIgnoreCase));

            if (dbEntry is null)
                return false;

            db.Aliases.Delete(dbEntry);
            aliases.TryRemove(dbEntry);

            return (await db.SaveChangesAsync()) is not 0;
        }

        /// <summary>
        /// Removes all aliases under the specified Discord server ID.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <returns><see langword="true"/> if the aliases were successfully removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> ClearAliasesAsync(CommandContext context)
        {
            if (context.Guild is null && !GeneralService.IsOwner(context, context.User.Id))
                return false;

            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            if (!db.Aliases.Cache.TryGetValue(context.Guild?.Id ?? default, out var aliases))
                return false;

            db.Aliases.DeleteRange(aliases.ToArray());
            aliases.Clear();

            return (await db.SaveChangesAsync()) is not 0;
        }

        /// <summary>
        /// Gets the aliases under the specified Discord server ID.
        /// </summary>
        /// <param name="sid">The ID of the Discord server, <see langword="null"/> to get global aliases.</param>
        /// <returns>A collection of aliases.</returns>
        public ConcurrentHashSet<AliasEntity> GetAliases(ulong? sid)
        {
            _dbCache.Aliases.TryGetValue(sid ?? default, out var aliases);
            return aliases ?? new(1, 0);
        }

        /// <summary>
        /// Removes trailing quotation marks and the command prefix from the command string.
        /// </summary>
        /// <param name="context">The context prefix.</param>
        /// <param name="command">The command string, with arguments if any.</param>
        /// <returns>The sanitized command string.</returns>
        public string SanitizeCommandString(string prefix, string command)
        {
            // Sanitize the command input
            if (command.StartsWith('"') && command.EndsWith('"'))
                command = command[1..^1];

            if (command.StartsWith(prefix))
                command = command.Remove(0, prefix.Length);

            return command;
        }
    }
}