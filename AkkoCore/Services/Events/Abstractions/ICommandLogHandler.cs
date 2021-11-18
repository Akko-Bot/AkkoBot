using DSharpPlus.CommandsNext;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events.Abstractions;

/// <summary>
/// Represents an object that handles log messages when a command is executed.
/// </summary>
public interface ICommandLogHandler
{
    /// <summary>
    /// Logs when a command has hit a cooldown limit.
    /// </summary>
    Task LogCmdCooldownAsync(CommandsNextExtension cmdHandler, CommandErrorEventArgs eventArgs);

    /// <summary>
    /// Logs exceptions thrown before or during command execution.
    /// </summary>
    Task LogCmdErrorAsync(CommandsNextExtension cmdHandler, CommandErrorEventArgs eventArgs);

    /// <summary>
    /// Logs basic information about command execution.
    /// </summary>
    Task LogCmdExecutionAsync(CommandsNextExtension cmdHandler, CommandExecutionEventArgs eventArgs);

    /// <summary>
    /// Logs exceptions thrown before or during the execution of a slash command.
    /// </summary>
    Task LogSlashCmdErrorAsync(SlashCommandsExtension slashHandler, SlashCommandErrorEventArgs eventArgs);

    /// <summary>
    /// Logs basic information about the execution of a slash command.
    /// </summary>
    Task LogSlashCmdExecutionAsync(SlashCommandsExtension slashHandler, SlashCommandExecutedEventArgs eventArgs);
}