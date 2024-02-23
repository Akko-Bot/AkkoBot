using AkkoBot.Core.Config.Abstractions;
using AkkoBot.Core.Logging.Models;
using AkkoBot.Discord.Events.Logging.Abstractions;
using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace AkkoBot.Discord.Events.Logging;

/// <summary>
/// Logs bot commands.
/// </summary>
[Service<ICommandLogger>(ServiceLifetime.Singleton)]
internal sealed class CommandLogger : ICommandLogger
{
    private readonly ILoggerLoader _loggerLoader;
    private readonly ILogger<CommandLogger> _logger;

    /// <summary>
    /// Logs bot commands.
    /// </summary>
    /// <param name="loggerLoader">The log loader.</param>
    /// <param name="logger">The logger to use.</param>
    public CommandLogger(ILoggerLoader loggerLoader, ILogger<CommandLogger> logger)
    {
        _loggerLoader = loggerLoader;
        _logger = logger;
    }

    /// <inheritdoc />
    [SuppressMessage("Style", "CA2254:Template should be a static expression",
        Justification = "The template does not change at runtime.")]
    public Task LogSuccessAsync(CommandsExtension cmdsExt, CommandExecutedEventArgs eventArgs)
    {
        var logArguments = new CommandLogArguments(eventArgs.Context);

        _logger.LogInformation(
            _loggerLoader.LogMessageTemplate,
            logArguments.GetLogArguments(_loggerLoader.LogMessageTemplate)
        );

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    [SuppressMessage("Style", "CA2254:Template should be a static expression",
        Justification = "The template does not change at runtime.")]
    public Task LogErrorAsync(CommandsExtension cmdsExt, CommandErroredEventArgs eventArgs)
    {
        var logArguments = new CommandLogArguments(eventArgs.Context, eventArgs.Exception);

        _logger.LogError(
            _loggerLoader.LogMessageTemplate,
            logArguments.GetLogArguments(_loggerLoader.LogMessageTemplate)
        );

        return Task.CompletedTask;
    }
}