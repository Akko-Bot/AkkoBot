using AkkoBot.Core;
using System;
using System.Threading;

namespace AkkoBot
{
    public class Program
    {
        public static bool RestartBot { get; internal set; } = true;
        public static CancellationTokenSource ShutdownToken { get; private set; }

        // Entry point.
        private static void Main()
        {
            Console.WriteLine($"Pid: {Environment.ProcessId}");

            // Start the bot.
            while (RestartBot)
            {
                ShutdownToken = new();
                var bot = new Bot(ShutdownToken.Token);

                // Run the bot
                bot.RunAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                // Clean-up
                bot.Dispose();
                ShutdownToken.Dispose();
            }
        }
    }
}