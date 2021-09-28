﻿using AkkoCore.Common;
using AkkoCore.Config;
using AkkoCore.Config.Abstractions;
using AkkoCore.Config.Models;
using AkkoCore.Core.Abstractions;
using AkkoCore.Core.Common;
using DSharpPlus;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AkkoCore.Core
{
    /// <summary>
    /// Represents a bot that connects to Discord.
    /// </summary>
    public class Bot : IDisposable
    {
        private BotCore _botCore;

        private readonly CancellationTokenSource _cTokenSource;
        private readonly IBotLifetime _lifetime;

        /// <summary>
        /// Initializes a Discord bot.
        /// </summary>
        /// <param name="cTokenSource">Cancellation token to signal the bot to stop running.</param>
        public Bot(CancellationTokenSource cTokenSource)
        {
            _cTokenSource = cTokenSource;
            _lifetime = new BotLifetime(cTokenSource);
        }

        /// <summary>
        /// Initializes the bot and connects it to Discord.
        /// </summary>
        public async Task<bool> RunAsync()
        {
            // Load up credentials
            var configLoader = new ConfigLoader();
            var creds = configLoader.LoadCredentials(AkkoEnvironment.CredsPath);
            var botConfig = configLoader.LoadConfig<BotConfig>(AkkoEnvironment.BotConfigPath);
            var logConfig = configLoader.LoadConfig<LogConfig>(AkkoEnvironment.LogConfigPath);

            try
            {
                // Initialize bot configuration
                _botCore = await new BotCoreBuilder(creds, botConfig, logConfig)
                    .WithSingletonService<IConfigLoader>(configLoader)
                    .WithSingletonService(_lifetime)
                    .WithDefaultLogging()
                    .WithDefaultServices()
                    .WithDefaultDbContext()
                    .BuildDefaultAsync();

                // Connect to Discord
                await _botCore.BotShardedClient.StartAsync();

                // Block the program until it is closed.
                await Task.Delay(Timeout.Infinite, _cTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine();

                if (_lifetime.RestartBot)
                {
                    _botCore.BotShardedClient.Logger.LogWarning(
                        new EventId(LoggerEvents.ConnectionClose.Id, "Restart"),
                        @"A restart has been requested."
                    );
                }
            }
            catch (NpgsqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine(
                    "An error has occurred while attempting to establish a connection with the database. " +
                    "Make sure your credentials are correct and that you don't have " +
                    "a firewall or any external software blocking the connection." + Environment.NewLine +
                    ex.Message
                );

                Console.ResetColor();
                Console.WriteLine(Environment.NewLine + "Press Enter to exit.");

                Console.ReadLine();
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

            return _lifetime.RestartBot;
        }

        public void Dispose()
        {
            _cTokenSource?.Dispose();
            _lifetime?.Dispose();
            _botCore?.Dispose();
            _botCore = null;

            GC.SuppressFinalize(this);
        }
    }
}