using AkkoCore.Commands.Common;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoCore.Commands.ArgumentConverters;

internal sealed class SmartStringConverter : IArgumentConverter<SmartString>
{
    public Task<Optional<SmartString>> ConvertAsync(string input, CommandContext ctx)
        => Task.FromResult(Optional.FromValue(new SmartString(ctx, input, true)));
}