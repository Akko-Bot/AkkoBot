using AkkoCore.Core;
using LinqToDB.EntityFrameworkCore;
using System;
using System.Threading;

namespace AkkoBot
{
    internal class Program
    {
        private static bool _restartBot = true;
        private static CancellationTokenSource? _shutdownToken;

        // Entry point.
        private static void Main()
        {
            // Print the process ID
            Console.WriteLine($"Pid: {Environment.ProcessId}");

            // LinqToDB initialization
            LinqToDBForEFTools.Initialize();

            // Start the bot.
            while (_restartBot)
            {
                _shutdownToken = new();
                var bot = new Bot(_shutdownToken);

                // Run the bot
                _restartBot = bot.RunAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                // Clean-up
                bot.Dispose();
                _shutdownToken.Dispose();
            }
        }
    }
}