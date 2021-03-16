using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AkkoBot.Extensions
{
    public static class CommandExt
    {
        /// <summary>
        /// Executes a command and logs its result to the console.
        /// </summary>
        /// <param name="command">This command.</param>
        /// <param name="context">The command context.</param>
        public static async Task ExecuteAndLogAsync(this Command command, CommandContext context)
        {
            var execution = await command.ExecuteAsync(context).ConfigureAwait(false);
            var level = (execution.IsSuccessful) ? LogLevel.Information : LogLevel.Error;

            context.Client.Logger.LogCommand(level, context, execution.Exception);
        }
    }
}