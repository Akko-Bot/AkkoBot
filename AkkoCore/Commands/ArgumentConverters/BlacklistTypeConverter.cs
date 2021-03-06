using AkkoCore.Services.Database.Enums;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoCore.Commands.ArgumentConverters;

internal sealed class BlacklistTypeConverter : IArgumentConverter<BlacklistType>
{
    public Task<Optional<BlacklistType>> ConvertAsync(string input, CommandContext ctx)
    {
        // Determine the appropriate type specified by the user
        return input?.ToLowerInvariant() switch
        {
            "u" or "user" or "users" => Task.FromResult(Optional.FromValue(BlacklistType.User)),
            "c" or "channel" or "channels" => Task.FromResult(Optional.FromValue(BlacklistType.Channel)),
            "s" or "server" or "servers" => Task.FromResult(Optional.FromValue(BlacklistType.Server)),
            _ => Task.FromResult(Optional.FromNoValue<BlacklistType>())
        };
    }
}