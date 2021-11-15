using AkkoCore.Models.Serializable;
using DSharpPlus.CommandsNext;
using System;
using System.Collections.Generic;

namespace AkkoCore.Commands.Abstractions;

/// <summary>
/// Represents an object that generates command help messages.
/// </summary>
public interface IHelpFormatter : IDisposable
{
    /// <summary>
    /// Defines whether the command was found or not.
    /// </summary>
    bool IsErroed { get; }

    /// <summary>
    /// Generates a help message for the command in the current context.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <returns>A command help message.</returns>
    SerializableDiscordMessage GenerateHelpMessage(CommandContext context);

    /// <summary>
    /// Generates a help message for the specified command string.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="inputCommand">The command string and its arguments.</param>
    /// <returns>A command help message.</returns>
    SerializableDiscordMessage GenerateHelpMessage(CommandContext context, IList<string> inputCommand);
}