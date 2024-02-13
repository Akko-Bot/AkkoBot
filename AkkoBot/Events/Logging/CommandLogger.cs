using AkkoBot.Events.Logging.Abstractions;
using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using Kotz.DependencyInjection;

namespace AkkoBot.Events.Logging;

/// <summary>
/// Logs bot commands.
/// </summary>
[Service<ICommandLogger>(ServiceLifetime.Singleton)]
internal sealed class CommandLogger : ICommandLogger
{
    private const string _defaultLogTemplate =
        """
        [Shard {ShardId}]
              User: {Username} [{UserId}]
              Server: {GuildName} {GuildId}
              Channel: #{ChannelName} [{ChannelId}]
              Command: {Command}
        """;
    private readonly ILogger<CommandLogger> _logger;

    /// <summary>
    /// Logs bot commands.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    public CommandLogger(ILogger<CommandLogger> logger)
        => _logger = logger;

    /// <inheritdoc />
    public Task LogSuccessAsync(CommandsExtension cmdsExt, CommandExecutedEventArgs eventArgs)
    {
        _logger.LogInformation(
            _defaultLogTemplate,
            cmdsExt.Client.ShardId,
            eventArgs.Context.User.Username,
            eventArgs.Context.User.Id,
            eventArgs.Context.Guild?.Name ?? "Private",
            (eventArgs.Context.Guild is null) ? string.Empty : "[" + eventArgs.Context.Guild.Id + "]",
            eventArgs.Context.Channel.Name ?? "Private",
            eventArgs.Context.Channel.Id,
            $"{eventArgs.Context.Command.Name} {string.Join(' ', eventArgs.Context.Arguments.Select(x => x.Value))}"
        );
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogErrorAsync(CommandsExtension cmdsExt, CommandErroredEventArgs eventArgs)
    {
        _logger.LogError(
            _defaultLogTemplate,
            cmdsExt.Client.ShardId,
            eventArgs.Context.User.Username,
            eventArgs.Context.User.Id,
            eventArgs.Context.Guild?.Name ?? "Private",
            (eventArgs.Context.Guild is null) ? string.Empty : "[" + eventArgs.Context.Guild.Id + "]",
            eventArgs.Context.Channel.Name ?? "Private",
            eventArgs.Context.Channel.Id,
            $"{eventArgs.Context.Command.Name} {string.Join(' ', eventArgs.Context.Arguments.Select(x => x.Value)) + Environment.NewLine + AnsiColor.Red + eventArgs.Exception}"
        );
        return Task.CompletedTask;
    }
}