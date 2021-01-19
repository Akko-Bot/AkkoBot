using System.Threading.Tasks;
using AkkoBot.Services.Database.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;

namespace AkkoBot.Command.ArgumentConverters
{
    public class BlacklistTypeConverter : IArgumentConverter<BlacklistType>
    {
        public Task<Optional<BlacklistType>> ConvertAsync(string blType, CommandContext ctx)
        {
            // Determine the appropriate type specified by the user
            return blType?.ToLowerInvariant() switch
            {
                "u" or "user" => Task.FromResult(Optional.FromValue(BlacklistType.User)),
                "c" or "channel" => Task.FromResult(Optional.FromValue(BlacklistType.Channel)),
                "s" or "server" => Task.FromResult(Optional.FromValue(BlacklistType.Server)),
                _ => Task.FromResult(Optional.FromValue(BlacklistType.Unspecified))
            };
        }
    }
}