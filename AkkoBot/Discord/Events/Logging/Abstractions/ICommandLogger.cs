using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;

namespace AkkoBot.Discord.Events.Logging.Abstractions;

/// <summary>
/// Represents an object that logs bot commands. 
/// </summary>
public interface ICommandLogger
{
    /// <summary>
    /// Logs a command that executed successfully.
    /// </summary>
    /// <param name="cmdsExt">The commands extension.</param>
    /// <param name="eventArgs">The event arguments.</param>
    Task LogSuccessAsync(CommandsExtension cmdsExt, CommandExecutedEventArgs eventArgs);

    /// <summary>
    /// Logs a command that failed to execute.
    /// </summary>
    /// <param name="cmdsExt">The commands extension.</param>
    /// <param name="eventArgs">The event arguments.</param>
    Task LogErrorAsync(CommandsExtension cmdsExt, CommandErroredEventArgs eventArgs);
}