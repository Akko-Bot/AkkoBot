using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IDbCacher _dbCache;
        private readonly DiscordShardedClient _clients;

        public CommandControlService(IServiceProvider services, IDbCacher dbCache, DiscordShardedClient clients)
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
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            // Don't let user disable gcmd
            if (cmd.Module.ModuleType.Name.Equals("GlobalCommandControl") || !db.BotConfig.AddDisabledCommand(cmd))
                return false;

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
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            if (!db.BotConfig.Cache.DisabledCommands.Contains(cmdString, StringComparison.InvariantCultureIgnoreCase, out var qualifiedName))
                return false;
            if (!db.BotConfig.RemoveDisabledCommand(qualifiedName, out var cmd))
                return false;

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
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);
            var cmdHandlers = (await _clients.GetCommandsNextAsync()).Values;

            foreach (var cmd in cmds)
            {
                if (db.BotConfig.AddDisabledCommand(cmd))
                {
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
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);
            var registrationMethod = typeof(CommandsNextExtension).GetMethod("AddToCommandDictionary", BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic);
            var cmds = db.BotConfig.DisabledCommandCache.Values.Where(x => x.Module.ModuleType.FullName.Contains(module, StringComparison.InvariantCultureIgnoreCase));

            foreach (var cmd in cmds)
            {
                if (db.BotConfig.RemoveDisabledCommand(cmd.QualifiedName, out var cachedCommand))
                {
                    foreach (var cmdHandler in (await _clients.GetCommandsNextAsync()).Values)
                        registrationMethod?.Invoke(cmdHandler, new object[] { cmd });
                }
            }

            return await db.SaveChangesAsync() is not 0 && registrationMethod is not null;
        }
    }
}