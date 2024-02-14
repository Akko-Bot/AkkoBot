using AkkoBot.Core.Config.Abstractions;
using AkkoBot.Core.Logging.Models;
using AkkoBot.Discord.Events.Logging.Abstractions;
using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using Kotz.DependencyInjection;

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
    public Task LogSuccessAsync(CommandsExtension cmdsExt, CommandExecutedEventArgs eventArgs)
    {
        var logArguments = new CommandLogArguments(eventArgs.Context);

#pragma warning disable CA2254
        _logger.LogInformation(
            _loggerLoader.LogMessageTemplate,
            logArguments.GetLogArguments(_loggerLoader.LogMessageTemplate)
        );
#pragma warning restore CA2254

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogErrorAsync(CommandsExtension cmdsExt, CommandErroredEventArgs eventArgs)
    {
        var logArguments = new CommandLogArguments(eventArgs.Context, eventArgs.Exception);

#pragma warning disable CA2254
        _logger.LogError(
            _loggerLoader.LogMessageTemplate,
            logArguments.GetLogArguments(_loggerLoader.LogMessageTemplate)
        );
#pragma warning restore CA2254

        return Task.CompletedTask;
    }
}