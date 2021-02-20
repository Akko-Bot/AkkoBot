using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Extensions
{
    public static class ILoggerExt
    {
        /// <summary>
        /// Logs command execution to the console.
        /// </summary>
        /// <param name="logger">This logger.</param>
        /// <param name="level">The severity level of the log.</param>
        /// <param name="context">The command context.</param>
        /// <param name="exception">The excetion that occurred during command execution.</param>
        public static Task LogCommand(this ILogger logger, LogLevel level, CommandContext context, Exception exception = null)
        {
            logger.BeginScope(context);

            logger.Log(
                level,
                new EventId(LoggerEvents.Misc.Id, "Command"),
                exception,
                context.Message.Content
            );

            return Task.CompletedTask;
        }
    }
}