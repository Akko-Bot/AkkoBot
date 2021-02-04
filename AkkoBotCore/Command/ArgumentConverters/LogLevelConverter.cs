using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AkkoBot.Command.ArgumentConverters
{
    public class LogLevelConverter : IArgumentConverter<LogLevel>
    {
        public Task<Optional<LogLevel>> ConvertAsync(string input, CommandContext ctx)
        {
            // Determine the appropriate type specified by the user
            return input?.ToLowerInvariant() switch
            {
                "debug" => Task.FromResult(Optional.FromValue(LogLevel.Debug)),
                "warn" or "warning" => Task.FromResult(Optional.FromValue(LogLevel.Warning)),
                "error" => Task.FromResult(Optional.FromValue(LogLevel.Error)),
                "crit" or "critical" => Task.FromResult(Optional.FromValue(LogLevel.Critical)),
                "nothing" or "none" => Task.FromResult(Optional.FromValue(LogLevel.None)),
                _ => Task.FromResult(Optional.FromValue(LogLevel.Information))
            };
        }
    }
}