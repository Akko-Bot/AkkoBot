using AkkoBot.Commands.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AkkoBot.Commands.Common
{
    /// <summary>
    /// Keeps track of commands and users on a global cooldown.
    /// </summary>
    internal class AkkoCooldown : ICommandCooldown
    {
        private readonly ConcurrentDictionary<string, TimeSpan> _globalCooldown = new();
        private readonly ConcurrentDictionary<(string, ulong), TimeSpan> _serverCooldown = new();
        private readonly ConcurrentDictionary<(string, ulong), DateTimeOffset> _recentUsers = new();

        /// <summary>
        /// Collection of qualified names of all global commands with a cooldown.
        /// </summary>
        public IEnumerable<KeyValuePair<string, TimeSpan>> Commands => _globalCooldown;

        /// <summary>
        /// Collection of qualified names of all server commands with a cooldown.
        /// </summary>
        public IEnumerable<KeyValuePair<(string, ulong), TimeSpan>> GuildCommands => _serverCooldown;

        /// <summary>
        /// Checks if the specified command has a cooldown.
        /// </summary>
        /// <param name="cmd">The command to be checked.</param>
        /// <param name="server">The Discord guild specific to the cooldown or <see langword="null"/> if it's a global cooldown.</param>
        /// <returns><see langword="true"/> if it has a cooldown, <see langword="false"/> otherwise.</returns>
        public bool ContainsCommand(Command cmd, DiscordGuild server = null)
        {
            return (server is null)
                ? _globalCooldown.ContainsKey(cmd.QualifiedName)
                : _globalCooldown.ContainsKey(cmd.QualifiedName) || _serverCooldown.ContainsKey((cmd.QualifiedName, server.Id));
        }

        /// <summary>
        /// Adds a command cooldown.
        /// </summary>
        /// <param name="cmd">The command to be added.</param>
        /// <param name="duration">The time the cooldown should last.</param>
        /// <param name="server">The Discord guild specific to the cooldown or <see langword="null"/> if it's a global cooldown.</param>
        /// <returns><see langword="true"/> if the cooldown was successfully added, <see langword="false"/> otherwise.</returns>
        public bool AddCommand(Command cmd, TimeSpan duration, DiscordGuild server = null)
        {
            return (server is null)
                ? _globalCooldown.TryAdd(cmd.QualifiedName, duration)
                : _serverCooldown.TryAdd((cmd.QualifiedName, server.Id), duration);
        }

        /// <summary>
        /// Removes a command cooldown.
        /// </summary>
        /// <param name="qualifiedCommand">The qualified name of the command to be removed.</param>
        /// <param name="sid">The ID of the Discord guild specific to the cooldown or <see langword="null"/> if it's a global cooldown.</param>
        /// <returns><see langword="true"/> if the cooldown was successfully removed, <see langword="false"/> otherwise.</returns>
        public bool RemoveCommand(string qualifiedCommand, ulong? sid)
        {
            return (!sid.HasValue)
                ? _globalCooldown.TryRemove(qualifiedCommand, out _)
                : _serverCooldown.TryRemove((qualifiedCommand, sid.Value), out _);
        }

        /// <summary>
        /// Adds a user to the cooldown list for a certain command.
        /// </summary>
        /// <param name="cmd">The command to be put on cooldown.</param>
        /// <param name="user">The user to be put on cooldown.</param>
        /// <returns><see langword="true"/> if the user was successfully added, <see langword="false"/> otherwise.</returns>
        public bool AddUser(Command cmd, DiscordUser user)
            => _recentUsers.TryAdd((cmd.QualifiedName, user.Id), DateTimeOffset.Now);

        /// <summary>
        /// Checks if a user has an active cooldown for the specified command.
        /// </summary>
        /// <param name="cmd">The command on cooldown.</param>
        /// <param name="user">The user to be checked.</param>
        /// <param name="server">The Discord guild specific to the cooldown or <see langword="null"/> if it's a global cooldown.</param>
        /// <remarks>In case a command has a global and a server cooldown, the longest will apply.</remarks>
        /// <returns><see langword="true"/> if the user has an active cooldown for <paramref name="cmd"/>, <see langword="false"/> otherwise.</returns>
        public bool IsOnCooldown(Command cmd, DiscordUser user, DiscordGuild server = null)
        {
            // Check cooldown
            if (IsOnGlobalCooldown(cmd, user) || IsOnServerCooldown(cmd, user, server))
                return true;

            _recentUsers.TryRemove((cmd.QualifiedName, user.Id), out _);
            return false;
        }

        /// <summary>
        /// Initializes this object with entries from the database.
        /// </summary>
        /// <param name="dbCommands">Collection of entries to be loaded into memory.</param>
        /// <returns>This object.</returns>
        public ICommandCooldown LoadFromEntities(IEnumerable<CommandCooldownEntity> dbCommands)
        {
            foreach (var dbCmd in dbCommands)
            {
                if (!dbCmd.GuildId.HasValue)
                    _globalCooldown.AddOrUpdate(dbCmd.Command, dbCmd.Cooldown, (_, _) => dbCmd.Cooldown);
                else
                    _serverCooldown.AddOrUpdate((dbCmd.Command, dbCmd.GuildId.Value), dbCmd.Cooldown, (_, _) => dbCmd.Cooldown);
            }

            return this;
        }

        /// <summary>
        /// Checks if the specified command and user are on a global cooldown.
        /// </summary>
        /// <param name="cmd">The command on cooldown.</param>
        /// <param name="user">The user to be checked.</param>
        /// <returns><see langword="true"/> if the user has an active global cooldown for <paramref name="cmd"/>, <see langword="false"/> otherwise.</returns>
        private bool IsOnGlobalCooldown(Command cmd, DiscordUser user)
        {
            return _globalCooldown.TryGetValue(cmd.QualifiedName, out var cooldown) 
                && _recentUsers.TryGetValue((cmd.QualifiedName, user.Id), out var time) 
                && DateTimeOffset.Now.Subtract(time) <= cooldown;
        }

        /// <summary>
        /// Checks if the specified command and user are on a server cooldown.
        /// </summary>
        /// <param name="cmd">The command on cooldown.</param>
        /// <param name="user">The user to be checked.</param>
        /// <param name="server">The Discord guild specific to the cooldown.</param>
        /// <returns><see langword="true"/> if the user has an active server cooldown for <paramref name="cmd"/>, <see langword="false"/> otherwise.</returns>
        private bool IsOnServerCooldown(Command cmd, DiscordUser user, DiscordGuild server)
        {
            return _serverCooldown.TryGetValue((cmd.QualifiedName, server?.Id ?? default), out var cooldown)
                && _recentUsers.TryGetValue((cmd.QualifiedName, user.Id), out var time)
                && DateTimeOffset.Now.Subtract(time) <= cooldown;
        }
    }
}
