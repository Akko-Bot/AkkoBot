using AkkoCore.Core;
using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore;
using LinqToDB.Mapping;
using System;
using System.Threading;

namespace AkkoBot
{
    internal class Program
    {
        private static bool _restartBot = true;
        private static CancellationTokenSource _shutdownToken;

        // Entry point.
        private static void Main()
        {
            LinqToDBForEFTools.Initialize();
            MappingSchema.Default.SetConvertExpression<ulong, DataParameter>(x => new DataParameter(null, (decimal)x));
            MappingSchema.Default.SetConvertExpression<ulong?, DataParameter>(x => new DataParameter(null, (decimal?)x));

            Console.WriteLine($"Pid: {Environment.ProcessId}");

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