using AkkoBot.Core;
using System;

namespace AkkoBot
{
    public class Program
    {
        // Entry point.
        private static void Main()
        {
            Console.WriteLine($"Pid: {Environment.ProcessId}");
            
            // Start the bot.
            new Bot().MainAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
