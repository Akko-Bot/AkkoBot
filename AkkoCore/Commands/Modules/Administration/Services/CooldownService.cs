using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Extensions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Queries;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using LinqToDB;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for manipulating <see cref="CommandCooldownEntity"/> objects.
    /// </summary>
    [CommandService(ServiceLifetime.Singleton)]
    public sealed class CooldownService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ICommandCooldown _cmdCooldown;

        public CooldownService(IServiceScopeFactory scopeFactory, ICommandCooldown cmdCooldown)
        {
            _scopeFactory = scopeFactory;
            _cmdCooldown = cmdCooldown;
        }

        /// <summary>
        /// Adds a command cooldown to the database.
        /// </summary>
        /// <param name="cmd">The command to be added.</param>
        /// <param name="time">For how long the cooldown should last.</param>
        /// <param name="server">The Discord guild associated with this cooldown, <see langword="null"/> if the cooldown is global.</param>
        /// <returns><see langword="true"/> if the cooldown was successfully added, <see langword="false"/> otherwise.</returns>
        public async Task<bool> AddCommandCooldownAsync(Command cmd, TimeSpan time, DiscordGuild? server = default)
        {
            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            var newEntry = new CommandCooldownEntity()
            {
                GuildIdFK = server?.Id,
                Command = cmd.QualifiedName,
                Cooldown = time
            };

            db.Upsert(newEntry);

            // Update the cache
            _cmdCooldown.RemoveCommand(cmd.QualifiedName, server?.Id);
            _cmdCooldown.AddCommand(cmd, time, server);

            return await db.SaveChangesAsync() is not 0;
        }

        /// <summary>
        /// Removes a command cooldown from the database.
        /// </summary>
        /// <param name="qualifiedCommand">The qualified name of the command.</param>
        /// <param name="id">The ID of the Discord guild associated with this cooldown, <see langword="null"/> if the cooldown is global.</param>
        /// <returns><see langword="true"/> if the cooldown was successfully removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemoveCommandCooldownAsync(string qualifiedCommand, ulong? id = default)
        {
            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            return _cmdCooldown.RemoveCommand(qualifiedCommand, id)
                && await db.CommandCooldown.DeleteAsync(x => x.Command == qualifiedCommand && x.GuildIdFK == id) is not 0;
        }

        /// <summary>
        /// Gets the list of commands with a cooldown.
        /// </summary>
        /// <param name="server">The Discord guild associated with the cooldowns, <see langword="null"/> if the cooldowns are global.</param>
        /// <returns>The collection of commands with a cooldown.</returns>
        public IEnumerable<KeyValuePair<string, TimeSpan>> GetCooldownCommands(DiscordGuild? server = default)
        {
            return (server is null)
                ? _cmdCooldown.GlobalCommands
                : _cmdCooldown.GuildCommands.Where(x => x.Key.Item2 == server.Id).Select(x => KeyValuePair.Create(x.Key.Item1, x.Value));
        }
    }
}