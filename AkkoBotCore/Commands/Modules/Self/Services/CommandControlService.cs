using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Self.Services
{
    /// <summary>
    /// Groups utility methods to register disabled commands to the database and the command handler.
    /// </summary>
    public class CommandControlService : ICommandService
    {
        private readonly IServiceProvider _services;
        private readonly IDbCache _dbCache;
        private readonly DiscordShardedClient _clients;

        public CommandControlService(IServiceProvider services, IDbCache dbCache, DiscordShardedClient clients)
        {
            _services = services;
            _dbCache = dbCache;
            _clients = clients;
        }

        /// <summary>
        /// Gets the disabled commands stored in the cache.
        /// </summary>
        /// <returns>A collection of all disabled commands.</returns>
        public ConcurrentDictionary<string, Command> GetDisabledCommands()
            => _dbCache.DisabledCommandCache;

        /// <summary>
        /// Disables a command globally.
        /// </summary>
        /// <param name="cmd">The command to be disabled.</param>
        /// <returns><see langword="true"/> if the command got disabled, <see langword="false"/> otherwise.</returns>
        public async Task<bool> DisableGlobalCommandAsync(Command cmd)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            // Don't let user disable gcmd
            if (cmd.Module.ModuleType.Name.Equals("GlobalCommandControl") || !_dbCache.DisabledCommandCache.TryAdd(cmd.QualifiedName, cmd))
                return false;

            var botConfig = await db.BotConfig.FirstOrDefaultAsync();
            botConfig.DisabledCommands.Add(cmd.QualifiedName);

            // Unregister the command
            foreach (var cmdHandler in (await _clients.GetCommandsNextAsync()).Values)
                cmdHandler.UnregisterCommands(cmd);

            return await db.SaveChangesAsync() is not 0;
        }

        /// <summary>
        /// Enables a command globally.
        /// </summary>
        /// <param name="cmdString">The qualified name of the command.</param>
        /// <returns><see langword="true"/> if the command got enabled, <see langword="false"/> otherwise.</returns>
        public async Task<bool> EnableGlobalCommandAsync(string cmdString)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            if (!_dbCache.DisabledCommandCache.Keys.Contains(cmdString, StringComparison.InvariantCultureIgnoreCase, out var qualifiedName))
                return false;
            if (!_dbCache.DisabledCommandCache.TryRemove(qualifiedName, out var cmd))
                return false;

            var botConfig = await db.BotConfig.FirstOrDefaultAsync();
            botConfig.DisabledCommands.Remove(cmd.QualifiedName);

            // Register the command - Reflection is needed because CommandsNextExtension doesn't have a sane registration method for commands that are already built
            var registrationMethod = typeof(CommandsNextExtension).GetMethod("AddToCommandDictionary", BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var cmdHandler in (await _clients.GetCommandsNextAsync()).Values)
                registrationMethod?.Invoke(cmdHandler, new object[] { cmd });

            return await db.SaveChangesAsync() is not 0 && registrationMethod is not null;
        }

        /// <summary>
        /// Disables multiple commands globally.
        /// </summary>
        /// <param name="cmds">The commands to be disabled.</param>
        /// <returns><see langword="true"/> if at least one command got disabled, <see langword="false"/> otherwise.</returns>
        public async Task<bool> DisableGlobalCommandsAsync(IEnumerable<Command> cmds)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);
            var cmdHandlers = (await _clients.GetCommandsNextAsync()).Values;
            var botConfig = await db.BotConfig.FirstOrDefaultAsync();

            foreach (var cmd in cmds)
            {
                if (_dbCache.DisabledCommandCache.TryAdd(cmd.QualifiedName, cmd))
                {
                    botConfig.DisabledCommands.Add(cmd.QualifiedName);

                    foreach (var cmdHandler in cmdHandlers)
                        cmdHandler.UnregisterCommands(cmd);
                }
            }

            return await db.SaveChangesAsync() is not 0;
        }

        /// <summary>
        /// Enables all commands from a module.
        /// </summary>
        /// <param name="module">Module the commands belong to.</param>
        /// <returns><see langword="true"/> if at least one command got enabled, <see langword="false"/> otherwise.</returns>
        public async Task<bool> EnableGlobalCommandsAsync(string module)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);
            var registrationMethod = typeof(CommandsNextExtension).GetMethod("AddToCommandDictionary", BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic);
            var cmds = _dbCache.DisabledCommandCache.Values.Where(x => x.Module.ModuleType.FullName.Contains(module, StringComparison.InvariantCultureIgnoreCase));
            var botConfig = await db.BotConfig.FirstOrDefaultAsync();

            foreach (var cmd in cmds)
            {
                if (_dbCache.DisabledCommandCache.TryRemove(cmd.QualifiedName, out var cachedCommand))
                {
                    botConfig.DisabledCommands.Remove(cachedCommand.QualifiedName);

                    foreach (var cmdHandler in (await _clients.GetCommandsNextAsync()).Values)
                        registrationMethod?.Invoke(cmdHandler, new object[] { cmd });
                }
            }

            return await db.SaveChangesAsync() is not 0 && registrationMethod is not null;
        }
    }
}