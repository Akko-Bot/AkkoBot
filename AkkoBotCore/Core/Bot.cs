using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using AkkoBot.Credential;
using AkkoBot.Core.Common;
using AkkoBot.Services;

namespace AkkoBot.Core
{
    public class Bot
    {
        private Credentials _creds = new();

        public async Task MainAsync(string[] args)
        {
            PrepareCredentials();

            // Initialize bot configuration
            var botCore = await new BotCoreBuilder()
                .WithCredentials(_creds)
                .WithDefaultLogging()
                .WithDefaultCmdServices()
                .WithDefaultDbContext()
                .BuildAsync();

            // Debug
            foreach (var handler in botCore.CommandExt.Values)
                handler.CommandErrored += (a, b) =>
                {
                    botCore.BotClient.Logger.LogError(
                        new EventId(LoggerEvents.WebSocketReceiveFailure.Id, "Command"),
                        b?.Exception.ToString());
                    return Task.CompletedTask;
                };

            // Connect to Discord
            try
            {
                await botCore.BotClient.StartAsync();
            }
            catch (Exception ex)
            {
                botCore.BotClient.Logger.LogCritical(
                    new EventId(LoggerEvents.ConnectionFailure.Id, "AkkoBot"),
                    "An error has occurred while attempting to connect to Discord. " +
                    "Make sure your credentials are correct and that you don't have " +
                    $"a firewall or any external software blocking the connection. [{ex.Message}]"
                );

                TerminateProgram("\n");
            }

            // Block the program until it is closed.
            await Task.Delay(Timeout.Infinite);
        }

        private void PrepareCredentials()
        {
            // Define the name of the credentials file and
            // check if token in the credentials is valid
            if (File.Exists(AkkoEnvironment.CredentialsPath))
                LoadCredentials(AkkoEnvironment.CredentialsPath);
            else
                CreateCredentials(AkkoEnvironment.CredentialsPath);
        }

        private void CreateCredentials(string filePath)
        {
            // Ensure the folder exists
            if (!Directory.Exists(AkkoEnvironment.CredsDirectory))
                Directory.CreateDirectory(AkkoEnvironment.CredsDirectory);

            // Serialize the default credentials into a new file.
            using (var writer = File.CreateText(filePath))
                new Serializer().Serialize(writer, _creds);

            // Print instructions to the user and gracefully terminate the program
            TerminateProgram(
                "A credentials file has been generated for you in \"./Creds/credentials.yaml\"\n" +
                "Please, add your data to it and restart the program."
            );
        }

        private void LoadCredentials(string filePath)
        {
            // Open the file and deserialize it.
            using (var reader = new StreamReader(File.OpenRead(filePath)))
                _creds = new Deserializer().Deserialize<Credentials>(reader);

            // Check if token is remotely valid.
            if (_creds.Token.Length < 50)
                TerminateProgram("Your token is probably invalid. Please, add a valid token.");
        }

        private void TerminateProgram(string message)
        {
            Console.WriteLine(message + "\nPress Enter to exit...");
            Console.Read();
            Environment.Exit(Environment.ExitCode);
        }
    }
}
