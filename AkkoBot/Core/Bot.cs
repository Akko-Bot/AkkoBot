using AkkoBot.Common;
using AkkoBot.Core.Common;
using AkkoEntities.Config;
using DSharpPlus;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AkkoBot.Core
{
    /// <summary>
    /// Represents a bot that connects to Discord.
    /// </summary>
    public class Bot : IDisposable
    {
        private BotCore _botCore;
        private readonly CancellationToken _cToken;

        /// <summary>
        /// Initializes a Discord bot.
        /// </summary>
        /// <param name="cToken">Cancellation token to signal the bot to stop running.</param>
        public Bot(CancellationToken cToken)
            => _cToken = cToken;

        /// <summary>
        /// Initializes the bot and connects it to Discord.
        /// </summary>
        public async Task RunAsync()
        {
            // Load up credentials
            var configLoader = new ConfigLoader();
            var creds = configLoader.LoadCredentials(AkkoEnvironment.CredsPath);
            var botConfig = configLoader.LoadConfig<BotConfig>(AkkoEnvironment.BotConfigPath);
            var logConfig = configLoader.LoadConfig<LogConfig>(AkkoEnvironment.LogConfigPath);

            // Initialize bot configuration
            _botCore = await new BotCoreBuilder(creds, botConfig, logConfig)
                .WithSingletonServices(configLoader)
                .WithDefaultLogging()
                .WithDefaultServices()
                .WithDefaultDbContext()
                .BuildDefaultAsync();

            await ConnectAsync(_cToken);
        }

        /// <summary>
        /// Connects this bot to Discord.
        /// </summary>
        /// <param name="cToken">Cancellation token to signal the bot to stop running.</param>
        private async Task ConnectAsync(CancellationToken cToken)
        {
            try
            {
                // Connect to Discord
                await _botCore.BotShardedClient.StartAsync();

                // Block the program until it is closed.
                await Task.Delay(Timeout.Infinite, cToken);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine();

                if (Program.RestartBot)
                {
                    _botCore.BotShardedClient.Logger.LogWarning(
                        new EventId(LoggerEvents.ConnectionClose.Id, "Restart"),
                        @"A restart has been requested."
                    );
                }
            }
            catch (Exception ex)
            {
                _botCore.BotShardedClient.Logger.LogError(
                    new EventId(LoggerEvents.ConnectionFailure.Id, "Startup"),
                    @"An error has occurred while attempting to connect to Discord. " +
                    @"Make sure your credentials are correct and that you don't have " +
                    $"a firewall or any external software blocking the connection. [{ex.Message}]\n\n" +
                    @"Press Enter to exit."
                );

                Console.ReadLine();
            }
        }

        public void Dispose()
        {
            _botCore?.Dispose();
            _botCore = null;

            GC.SuppressFinalize(this);
        }
    }
}