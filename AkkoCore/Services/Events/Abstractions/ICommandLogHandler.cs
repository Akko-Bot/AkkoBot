using DSharpPlus.CommandsNext;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events.Abstractions
{
    /// <summary>
    /// Represents an object that handles log messages when a command is executed.
    /// </summary>
    public interface ICommandLogHandler
    {
        /// <summary>
        /// Logs exceptions thrown before or during command execution.
        /// </summary>
        Task LogCmdErrorAsync(CommandsNextExtension cmdHandler, CommandErrorEventArgs eventArgs);

        /// <summary>
        /// Logs basic information about command execution.
        /// </summary>
        Task LogCmdExecutionAsync(CommandsNextExtension cmdHandler, CommandExecutionEventArgs eventArgs);
    }
}