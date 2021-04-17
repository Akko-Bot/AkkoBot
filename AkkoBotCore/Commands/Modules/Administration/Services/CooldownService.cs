using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Database.Queries;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for manipulating <see cref="CommandCooldownEntity"/> objects.
    /// </summary>
    public class CooldownService : ICommandService
    {
        private readonly IServiceProvider _services;
        private readonly ICommandCooldown _cmdCooldown;

        public CooldownService(IServiceProvider services, ICommandCooldown cmdCooldown)
        {
            _services = services;
            _cmdCooldown = cmdCooldown;
        }

        /// <summary>
        /// Adds a command cooldown to the database.
        /// </summary>
        /// <param name="cmd">The command to be added.</param>
        /// <param name="time">For how long the cooldown should last.</param>
        /// <param name="server">The Discord guild associated with this cooldown, <see langword="null"/> if the cooldown is global.</param>
        /// <returns><see langword="true"/> if the cooldown was successfully added, <see langword="false"/> otherwise.</returns>
        public async Task<bool> AddCommandCooldownAsync(Command cmd, TimeSpan time, DiscordGuild server = null)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var newEntry = new CommandCooldownEntity()
            {
                GuildId = server?.Id,
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
        /// <param name="server">The Discord guild associated with this cooldown, <see langword="null"/> if the cooldown is global.</param>
        /// <returns><see langword="true"/> if the cooldown was successfully removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemoveCommandCooldownAsync(string qualifiedCommand, DiscordGuild server = null)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var dbEntry = (server is null)
                ? await db.CommandCooldown.FirstOrDefaultAsync(x => x.Command == qualifiedCommand)
                : await db.CommandCooldown.FirstOrDefaultAsync(x => x.Command == qualifiedCommand && x.GuildId == server.Id);

            if (dbEntry is null)
                return false;

            db.Remove(dbEntry);
            _cmdCooldown.RemoveCommand(dbEntry.Command, server?.Id);

            return await db.SaveChangesAsync() is not 0;
        }

        /// <summary>
        /// Gets the list of commands with a cooldown.
        /// </summary>
        /// <param name="server">The Discord guild associated with the cooldowns, <see langword="null"/> if the cooldowns are global.</param>
        /// <returns>The collection of commands with a cooldown.</returns>
        public IEnumerable<KeyValuePair<string, TimeSpan>> GetCooldownCommands(DiscordGuild server = null)
        {
            return (server is null)
                ? _cmdCooldown.Commands
                : _cmdCooldown.GuildCommands.Where(x => x.Key.Item2 == server.Id).Select(x => KeyValuePair.Create(x.Key.Item1, x.Value));
        }
    }
}
