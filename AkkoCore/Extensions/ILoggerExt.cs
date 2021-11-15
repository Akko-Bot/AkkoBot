using AkkoCore.Config.Models;
using AkkoCore.Services.Logging;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace AkkoCore.Extensions;

public static class ILoggerExt
{
    private static readonly EventId _commandEvent = new(LoggerEvents.Misc.Id, "Command");
    private static readonly EventId _slashCommandEvent = new(LoggerEvents.Misc.Id, "SlashCommand");

    /// <summary>
    /// Logs command execution to the console.
    /// </summary>
    /// <param name="logger">This logger.</param>
    /// <param name="level">The severity level of the log.</param>
    /// <param name="context">The command context.</param>
    /// <param name="message">An optional message to be shown on the log's header.</param>
    /// <param name="exception">The excetion that occurred during command execution.</param>
    public static void LogCommand(this ILogger logger, LogLevel level, CommandContext context, string message = "", Exception? exception = default)
    {
        var logConfig = context.Services.GetRequiredService<LogConfig>();

        logger.Log(
            level,
            _commandEvent,
            new LogData(context, message),
            exception,
            LogStrategy.GetLogStrategy(logConfig.LogFormat)
        );
    }

    /// <summary>
    /// Logs command execution to the console.
    /// </summary>
    /// <param name="logger">This logger.</param>
    /// <param name="level">The severity level of the log.</param>
    /// <param name="context">The interaction context.</param>
    /// <param name="message">An optional message to be shown on the log's header.</param>
    /// <param name="exception">The excetion that occurred during command execution.</param>
    public static void LogCommand(this ILogger logger, LogLevel level, InteractionContext context, string message = "", Exception? exception = default)
    {
        var logConfig = context.Services.GetRequiredService<LogConfig>();

        logger.Log(
            level,
            _slashCommandEvent,
            new LogData(context, message),
            exception,
            LogStrategy.GetLogStrategy(logConfig.LogFormat)
        );
    }
}