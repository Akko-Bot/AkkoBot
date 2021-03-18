using AkkoBot.Commands.Common;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoBot.Commands.ArgumentConverters
{
    class SmartStringConverter : IArgumentConverter<SmartString>
    {
        public Task<Optional<SmartString>> ConvertAsync(string input, CommandContext ctx)
            => Task.FromResult(Optional.FromValue(new SmartString(ctx, input)));
    }
}
