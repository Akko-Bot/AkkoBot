using AkkoBot.Config;
using AkkoBot.Models;
using AkkoBot.Services.Logging;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

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
        /// <param name="message">An optional message to be shown on the log's header.</param>
        /// <param name="exception">The excetion that occurred during command execution.</param>
        public static void LogCommand(this ILogger logger, LogLevel level, CommandContext context, string message = "", Exception exception = null)
        {
            var logConfig = context.Services.GetService<LogConfig>();

            logger.Log(
                level,
                new EventId(LoggerEvents.Misc.Id, "Command"),
                new LogData(context, message),
                exception,
                LogStrategy.GetLogStrategy(logConfig.LogFormat)
            );
        }
    }
}