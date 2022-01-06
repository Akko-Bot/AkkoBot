using AkkoCore.Services.Database.Enums;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.ArgumentConverters;

internal sealed class GuildLogConverter : IArgumentConverter<GuildLogType>
{
    public Task<Optional<GuildLogType>> ConvertAsync(string input, CommandContext ctx)
    {
        var result = Enum.GetValues<GuildLogType>().FirstOrDefault(x => input.Equals(x.ToString()));

        return (result is GuildLogType.None)
            ? Task.FromResult(Optional.FromNoValue<GuildLogType>())
            : Task.FromResult(Optional.FromValue(result));
    }
}