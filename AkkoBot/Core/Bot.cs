using AkkoBot.Core.Config.Models;
using AkkoBot.Core.Services.Abstractions;
using AkkoBot.Discord.Events.Logging.Abstractions;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors;
using DSharpPlus.Commands.Processors.MessageCommands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Processors.UserCommands;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace AkkoBot.Core;

/// <summary>
/// Represents a bot that connects to Discord.
/// </summary>
public sealed class Bot : BackgroundService
{
    private readonly DiscordConfiguration _clientConfig;
    private readonly CommandsConfiguration _cmdExtConfig;
    private readonly ICommandLogger _cmdLogger;
    private readonly IBotLifetime _lifetime;
    private readonly ILogger<Bot> _logger;

    /// <summary>
    /// Represents a bot that connects to Discord.
    /// </summary>
    /// <param name="creds">The bot credentials.</param>
    /// <param name="botConfig">The bot settings.</param>
    /// <param name="cmdLogger">The bot logger service.</param>
    /// <param name="lifetime">The bot lifetime.</param>
    /// <param name="logger">The host logger.</param>
    /// <param name="loggerFactory">The host logger factory.</param>
    /// <param name="serviceProvider">The host IoC container.</param>
    public Bot(Credentials creds, BotConfig botConfig, ICommandLogger cmdLogger, IBotLifetime lifetime, ILogger<Bot> logger, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
    {
        _clientConfig = new DiscordConfiguration()
        {
            Intents = DiscordIntents.All,                       // Sign up for all intents. Forgetting to enable them on the developer portal will throw an exception!
            Token = creds.Token,                               // Sets the bot token
            TokenType = TokenType.Bot,                          // Defines the type of token; User = 0, Bot = 1, Bearer = 2
            AutoReconnect = true,                               // Sets whether the bot should automatically reconnect in case it disconnects
            ReconnectIndefinitely = false,                      // Sets whether the bot should attempt to reconnect indefinitely
            MessageCacheSize = botConfig.MessageSizeCache,     // Defines how many messages should be cached per DiscordClient
            LoggerFactory = loggerFactory                      // Overrides D#+ default logger with my own
        };

        _cmdExtConfig = new CommandsConfiguration()
        {
            DebugGuildId = 805138211138437151,
            UseDefaultCommandErrorHandler = true,
            ServiceProvider = serviceProvider
        };
        _cmdLogger = cmdLogger;
        _lifetime = lifetime;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken cToken)
    {
        var assembly = Assembly.GetExecutingAssembly();

        do
        {
            var client = new DiscordShardedClient(_clientConfig);
            await client.UseCommandsAsync(_cmdExtConfig);
            var processors = new ICommandProcessor[]
            {
                new TextCommandProcessor(),
                new SlashCommandProcessor(),
                new UserCommandProcessor(),
                new MessageCommandProcessor()
            };

            foreach (var (_, cmdsExtension) in await client.UseCommandsAsync(_cmdExtConfig))
            {
                cmdsExtension.CommandExecuted += _cmdLogger.LogSuccessAsync;
                cmdsExtension.CommandErrored += _cmdLogger.LogErrorAsync;

                await cmdsExtension.AddProcessorsAsync(processors);
                cmdsExtension.AddCommands(assembly);
            }

            _logger.LogInformation("Starting AkkoBot v{Version}", AkkoConstants.BotVersion);

            await client.StartAsync();
            await Task.WhenAny(
                Task.Delay(Timeout.Infinite, _lifetime.StopToken),
                Task.Delay(Timeout.Infinite, cToken)
            );

            if (_lifetime.IsRestarting && !cToken.IsCancellationRequested)
                _logger.LogInformation("Application is restarting...");

        } while (_lifetime.IsRestarting && !cToken.IsCancellationRequested);
    }
}