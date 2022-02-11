using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoCore.Commands.ArgumentConverters;

internal sealed class SnowflakeObjectConverter : IArgumentConverter<SnowflakeObject>
{
    public async Task<Optional<SnowflakeObject>> ConvertAsync(string input, CommandContext ctx)
    {
        try { return Optional.FromValue((SnowflakeObject)await ctx.CommandsNext.ConvertArgument<DiscordChannel>(input, ctx)); } catch { }

        try { return Optional.FromValue((SnowflakeObject)await ctx.CommandsNext.ConvertArgument<DiscordRole>(input, ctx)); } catch { }

        try { return Optional.FromValue((SnowflakeObject)await ctx.CommandsNext.ConvertArgument<DiscordMember>(input, ctx)); } catch { }

        try { return Optional.FromValue((SnowflakeObject)await ctx.CommandsNext.ConvertArgument<DiscordUser>(input, ctx)); } catch { }

        return Optional.FromNoValue<SnowflakeObject>();
    }
}