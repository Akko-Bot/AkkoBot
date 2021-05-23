using AkkoBot.Common;
using AkkoBot.Config;
using AkkoBot.Core.Common;
using AkkoBot.Services.Logging.Abstractions;
using DSharpPlus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AkkoBot.Core
{
    public class Bot
    {
        public static CancellationTokenSource ShutdownToken { get; } = new();

        public async Task MainAsync()
        {
            // Load up credentials
            var configLoader = new ConfigLoader();
            var creds = configLoader.GetCredentials(AkkoEnvironment.CredsPath);
            var botConfig = configLoader.GetConfig<BotConfig>(AkkoEnvironment.BotConfigPath);
            var logConfig = configLoader.GetConfig<LogConfig>(AkkoEnvironment.LogConfigPath);

            // Initialize bot configuration
            var botCore = await new BotCoreBuilder(creds, botConfig, logConfig)
                .WithSingletonServices(configLoader)
                .WithDefaultLogging()
                .WithDefaultServices()
                .WithDefaultDbContext()
                .BuildDefaultAsync();

            try
            {
                // Connect to Discord
                await botCore.BotShardedClient.StartAsync();

                // Block the program until it is closed.
                await Task.Delay(Timeout.Infinite, ShutdownToken.Token);
            }
            catch (TaskCanceledException)
            {
                botCore.CommandExt[0].Services.GetService<IAkkoLoggerProvider>()?.Dispose();
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                botCore.BotShardedClient.Logger.LogError(
                    new EventId(LoggerEvents.ConnectionFailure.Id, "Startup"),
                    @"An error has occurred while attempting to connect to Discord. " +
                    @"Make sure your credentials are correct and that you don't have " +
                    $"a firewall or any external software blocking the connection. [{ex.Message}]\n\n" +
                    @"Press Enter to exit."
                );

                Console.ReadLine();
            }
        }
    }
}