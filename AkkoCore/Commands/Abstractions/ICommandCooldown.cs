using AkkoCore.Services.Database.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;

namespace AkkoCore.Commands.Abstractions;

/// <summary>
/// Defines an object that keeps track of commands and users on a global and/or guild cooldown.
/// </summary>
public interface ICommandCooldown : IDisposable
{
    /// <summary>
    /// Collection of qualified names of all global commands with a cooldown.
    /// </summary>
    IReadOnlyDictionary<string, TimeSpan> GlobalCommands { get; }

    /// <summary>
    /// Collection of qualified names of all server commands with a cooldown.
    /// </summary>
    IReadOnlyDictionary<(string, ulong), TimeSpan> GuildCommands { get; }

    /// <summary>
    /// Checks if the specified command has a cooldown.
    /// </summary>
    /// <param name="cmd">The command to be checked.</param>
    /// <param name="server">The Discord guild specific to the cooldown or <see langword="null"/> if it's a global cooldown.</param>
    /// <returns><see langword="true"/> if it has a cooldown, <see langword="false"/> otherwise.</returns>
    bool ContainsCommand(Command cmd, DiscordGuild? server = default);

    /// <summary>
    /// Adds a command cooldown.
    /// </summary>
    /// <param name="cmd">The command to be added.</param>
    /// <param name="duration">The time the cooldown should last.</param>
    /// <param name="server">The Discord guild specific to the cooldown or <see langword="null"/> if it's a global cooldown.</param>
    /// <returns><see langword="true"/> if the cooldown was successfully added, <see langword="false"/> otherwise.</returns>
    bool AddCommand(Command cmd, TimeSpan duration, DiscordGuild? server = default);

    /// <summary>
    /// Removes a command cooldown.
    /// </summary>
    /// <param name="qualifiedCommand">The qualified name of the command to be removed.</param>
    /// <param name="sid">The ID of the Discord guild specific to the cooldown or <see langword="null"/> if it's a global cooldown.</param>
    /// <returns><see langword="true"/> if the cooldown was successfully removed, <see langword="false"/> otherwise.</returns>
    bool RemoveCommand(string qualifiedCommand, ulong? sid);

    /// <summary>
    /// Adds a user to the cooldown list for a certain command.
    /// </summary>
    /// <param name="cmd">The command to be put on cooldown.</param>
    /// <param name="user">The user to be put on cooldown.</param>
    /// <returns><see langword="true"/> if the user was successfully added, <see langword="false"/> otherwise.</returns>
    bool AddUser(Command cmd, DiscordUser user);

    /// <summary>
    /// Checks if a user has an active cooldown for the specified command.
    /// </summary>
    /// <param name="cmd">The command on cooldown.</param>
    /// <param name="user">The user to be checked.</param>
    /// <param name="server">The Discord guild specific to the cooldown or <see langword="null"/> if it's a global cooldown.</param>
    /// <remarks>In case a command has a global and a server cooldown, the longest will apply.</remarks>
    /// <returns><see langword="true"/> if the user has an active cooldown for <paramref name="cmd"/>, <see langword="false"/> otherwise.</returns>
    bool IsOnCooldown(Command cmd, DiscordUser user, DiscordGuild? server = default);

    /// <summary>
    /// Initializes this object with entries from the database.
    /// </summary>
    /// <param name="dbCommands">Collection of entries to be loaded into memory.</param>
    /// <returns>This <see cref="ICommandCooldown"/>.</returns>
    ICommandCooldown LoadFromEntities(IEnumerable<CommandCooldownEntity> dbCommands);

    /// <summary>
    /// Removes command cooldown with entries from the database.
    /// </summary>
    /// <param name="dbCommands">Collection of entries to be loaded into memory.</param>
    /// <returns>This <see cref="ICommandCooldown"/>.</returns>
    ICommandCooldown UnloadFromEntities(IEnumerable<CommandCooldownEntity> dbCommands);
}