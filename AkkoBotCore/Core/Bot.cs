using AkkoBot.Common;
using AkkoBot.Core.Common;
using AkkoBot.Credential;
using AkkoBot.Extensions;
using DSharpPlus;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace AkkoBot.Core
{
    public class Bot
    {
        public static CancellationTokenSource ShutdownToken { get; } = new();

        public async Task MainAsync()
        {
            // Load up credentials
            var creds = PrepareCredentials(AkkoEnvironment.CredsPath);

            // Initialize bot configuration
            var botCore = await new BotCoreBuilder()
                .WithCredentials(creds)
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
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                botCore.BotShardedClient.Logger.LogError(
                    new EventId(LoggerEvents.ConnectionFailure.Id, "Startup"),
                    @"An error has occurred while attempting to connect to Discord. " +
                    @"Make sure your credentials are correct and that you don't have " +
                    $"a firewall or any external software blocking the connection. [{ex.Message}]"
                );

                TerminateProgram(Environment.NewLine);
            }
        }

        /// <summary>
        /// Prepare the credentials for bot startup.
        /// </summary>
        /// <param name="filePath">Path to the credentials file, with its name and extension.</param>
        /// <returns>A valid <see cref="Credentials"/> object.</returns>
        private Credentials PrepareCredentials(string filePath)
        {
            while (!IsValidCredential(filePath)) ;

            return LoadCredentials(filePath);
        }

        /// <summary>
        /// Creates the credentials file and the directory(ies) it is stored at.
        /// </summary>
        /// <param name="filePath">Path to the credentials file, with its name and extension.</param>
        private void CreateCredentials(string filePath)
        {
            // Ensure the folder exists
            if (!Directory.Exists(AkkoEnvironment.GetFileDirectoryPath(filePath)))
                Directory.CreateDirectory(AkkoEnvironment.GetFileDirectoryPath(filePath));

            // Serialize the default credentials into a new file.
            using var writer = File.CreateText(filePath);
            new Credentials().ToYaml(writer, new Serializer());
        }

        /// <summary>
        /// Ensures the credentials file exists and deserializes it into a <see cref="Credentials"/> object.
        /// </summary>
        /// <param name="filePath">Path to the credentials file, with its name and extension.</param>
        /// <returns>A <see cref="Credentials"/> object.</returns>
        private Credentials LoadCredentials(string filePath)
        {
            // If directory or file don't exist, return false
            if (!Directory.Exists(AkkoEnvironment.GetFileDirectoryPath(filePath)) || !File.Exists(filePath))
            {
                CreateCredentials(filePath);

                PauseProgram(
                    @"A credentials file has been generated for you at " +
                    AkkoEnvironment.CredsPath + Environment.NewLine +
                    @"Please, add your data to it and"
                );
            }

            // Open the file and deserialize it.
            using var reader = new StreamReader(File.OpenRead(filePath));
            return reader.FromYaml<Credentials>(new Deserializer());
        }

        /// <summary>
        /// Checks if the credentials file is remotely valid.
        /// </summary>
        /// <param name="filePath">Path to the credentials file, with its name and extension.</param>
        /// <returns><see langword="true"/> if it seems to be valid, <see langword="false"/> otherwise.</returns>
        private bool IsValidCredential(string filePath)
        {
            // Open the file and deserialize it.
            var creds = LoadCredentials(filePath);

            // Check if token and database password are remotely valid.
            if (creds.Token.Length < 50)
            {
                PauseProgram(
                    "Your token is probably invalid." + Environment.NewLine +
                    "Please, add a valid token and"
                );

                return false;
            }
            else if (creds.Database["Password"] == "postgres_password_here")
            {
                PauseProgram(
                    "You forgot to set your database password." + Environment.NewLine +
                    "Please, add it and"
                );

                return false;
            }

            return true;
        }

        /// <summary>
        /// Pauses the program for the user to read the <paramref name="message"/>.
        /// </summary>
        /// <param name="message">A message to be displayed to the user.</param>
        private void PauseProgram(string message)
        {
            Console.WriteLine(message + " press Enter when you are ready.");
            Console.ReadLine();
        }

        /// <summary>
        /// Terminates the program with a <paramref name="message"/> for the user to read.
        /// </summary>
        /// <param name="message">A message to be displayed to the user.</param>
        private void TerminateProgram(string message)
        {
            Console.WriteLine(message + Environment.NewLine + "Press Enter to exit...");
            Console.ReadLine();
            Environment.Exit(Environment.ExitCode);
        }
    }
}