using AkkoCore.Commands.Attributes;
using AkkoCore.Common;
using AkkoCore.Config.Abstractions;
using AkkoCore.Config.Models;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Self.Services
{
    /// <summary>
    /// Groups utility methods to register disabled commands to the database and the command handler.
    /// </summary>
    [CommandService(ServiceLifetime.Singleton)]
    public sealed class CommandControlService
    {
        private readonly MethodInfo _registrationMethod = typeof(CommandsNextExtension).GetMethod("AddToCommandDictionary", BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly IAkkoCache _akkoCache;
        private readonly IConfigLoader _configLoader;
        private readonly DiscordShardedClient _clients;
        private readonly BotConfig _botConfig;

        public CommandControlService(IAkkoCache akkoCache, IConfigLoader configLoader, DiscordShardedClient clients, BotConfig botConfig)
        {
            _akkoCache = akkoCache;
            _configLoader = configLoader;
            _clients = clients;
            _botConfig = botConfig;
        }

        /// <summary>
        /// Gets the disabled commands stored in the cache.
        /// </summary>
        /// <returns>A collection of all disabled commands.</returns>
        public ConcurrentDictionary<string, Command> GetDisabledCommands()
            => _akkoCache.DisabledCommandCache;

        /// <summary>
        /// Disables a command globally.
        /// </summary>
        /// <param name="cmd">The command to be disabled.</param>
        /// <returns><see langword="true"/> if the command got disabled, <see langword="false"/> otherwise.</returns>
        public async Task<bool> DisableGlobalCommandAsync(Command cmd)
        {
            // Don't let user disable gcmd
            if (cmd.Module.ModuleType.Name.Equals("GlobalCommandControl") || !_akkoCache.DisabledCommandCache.TryAdd(cmd.QualifiedName, cmd))
                return false;

            var result = _botConfig.DisabledCommands.Add(cmd.QualifiedName);

            // Unregister the command
            foreach (var cmdHandler in (await _clients.GetCommandsNextAsync()).Values)
                cmdHandler.UnregisterCommands(cmd);

            _configLoader.SaveConfig(_botConfig, AkkoEnvironment.BotConfigPath);

            return result;
        }

        /// <summary>
        /// Enables a command globally.
        /// </summary>
        /// <param name="cmdString">The qualified name of the command.</param>
        /// <returns><see langword="true"/> if the command got enabled, <see langword="false"/> otherwise.</returns>
        public async Task<bool> EnableGlobalCommandAsync(string cmdString)
        {
            if (!_akkoCache.DisabledCommandCache.Keys.Equals(cmdString, StringComparison.InvariantCultureIgnoreCase, out var qualifiedName))
                return false;
            if (!_akkoCache.DisabledCommandCache.TryRemove(qualifiedName, out var cmd))
                return false;

            var result = _botConfig.DisabledCommands.TryRemove(cmd.QualifiedName);

            // Register the command - Reflection is needed because CommandsNextExtension doesn't have a sane registration method for commands that are already built
            foreach (var cmdHandler in (await _clients.GetCommandsNextAsync()).Values)
                _registrationMethod?.Invoke(cmdHandler, new object[] { cmd });

            _configLoader.SaveConfig(_botConfig, AkkoEnvironment.BotConfigPath);

            return result;
        }

        /// <summary>
        /// Disables multiple commands globally.
        /// </summary>
        /// <param name="cmds">The commands to be disabled.</param>
        /// <returns><see langword="true"/> if at least one command got disabled, <see langword="false"/> otherwise.</returns>
        public async Task<bool> DisableGlobalCommandsAsync(IEnumerable<Command> cmds)
        {
            var cmdHandlers = (await _clients.GetCommandsNextAsync()).Values;
            var result = false;

            foreach (var cmd in cmds)
            {
                if (_akkoCache.DisabledCommandCache.TryAdd(cmd.QualifiedName, cmd))
                {
                    result |= _botConfig.DisabledCommands.Add(cmd.QualifiedName);

                    foreach (var cmdHandler in cmdHandlers)
                        cmdHandler.UnregisterCommands(cmd);
                }
            }

            if (result)
                _configLoader.SaveConfig(_botConfig, AkkoEnvironment.BotConfigPath);

            return result;
        }

        /// <summary>
        /// Enables all commands from a module.
        /// </summary>
        /// <param name="module">Module the commands belong to.</param>
        /// <returns><see langword="true"/> if at least one command got enabled, <see langword="false"/> otherwise.</returns>
        public async Task<bool> EnableGlobalCommandsAsync(string module)
        {
            var cmdHandlers = (await _clients.GetCommandsNextAsync()).Values;
            var cmds = _akkoCache.DisabledCommandCache.Values.Where(x => x.Module.ModuleType.FullName.Contains(module, StringComparison.InvariantCultureIgnoreCase));
            var result = false;

            foreach (var cmd in cmds)
            {
                if (_akkoCache.DisabledCommandCache.TryRemove(cmd.QualifiedName, out var cachedCommand))
                {
                    result |= _botConfig.DisabledCommands.TryRemove(cachedCommand.QualifiedName);

                    foreach (var cmdHandler in cmdHandlers)
                        _registrationMethod?.Invoke(cmdHandler, new object[] { cmd });
                }
            }

            if (result)
                _configLoader.SaveConfig(_botConfig, AkkoEnvironment.BotConfigPath);

            return result;
        }
    }
}