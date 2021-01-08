using AkkoBot.Core;
using System;

namespace AkkoBot
{
    public class Program
    {
        // Entry point.
        private static void Main(string[] args)
        {
            Console.WriteLine($"Pid: {Environment.ProcessId}");
            
            // Start the bot.
            new Bot().MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
